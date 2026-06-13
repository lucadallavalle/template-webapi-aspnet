using Microsoft.EntityFrameworkCore;
using WebApiTemplate.Core.Persistence;

namespace WebApiTemplate.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core implementation of <see cref="IUnitOfWorkFactory"/> that runs the
/// caller-provided unit of work inside a database transaction managed by EF Core's
/// configured execution strategy.
/// </summary>
/// <remarks>
/// With Npgsql the default execution strategy does not retry. Opt into connection resiliency by
/// configuring <c>UseNpgsql(..., o =&gt; o.EnableRetryOnFailure())</c>; when enabled, the delegate
/// may be re-executed with a fresh <see cref="DbContext"/> on transient failures, as required by
/// EF Core's connection-resiliency model, so it must be idempotent.
/// See https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency.
/// </remarks>
/// <typeparam name="TDbContext">The database context type.</typeparam>
/// <param name="dbContextFactory">The factory used to create a fresh context per attempt.</param>
/// <param name="serviceProvider">The provider passed to each <see cref="UnitOfWork{TDbContext}"/>.</param>
public sealed class UnitOfWorkFactory<TDbContext>(
    IDbContextFactory<TDbContext> dbContextFactory,
    IServiceProvider serviceProvider
) : IUnitOfWorkFactory
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public Task ExecuteInTransactionAsync(
        Func<IUnitOfWork, CancellationToken, Task> work,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(work);

        return ExecuteInTransactionAsync<object?>(
            async (uow, ct) =>
            {
                await work(uow, ct);
                return null;
            },
            cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<IUnitOfWork, CancellationToken, Task<TResult>> work,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(work);

        // The "sentinel" context is only used to obtain the configured execution strategy.
        // Each retry attempt creates its own fresh DbContext below to avoid leaking change-tracker state.
        await using var sentinelContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken
        );
        var strategy = sentinelContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(
            state: (Factory: dbContextFactory, ServiceProvider: serviceProvider, Work: work),
            operation: static async (state, ct) =>
            {
                await using var dbContext = await state.Factory.CreateDbContextAsync(ct);
                await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

                var unitOfWork = new UnitOfWork<TDbContext>(dbContext, state.ServiceProvider);
                var result = await state.Work(unitOfWork, ct);

                // Flush any changes still staged in the change tracker after the delegate returns,
                // so callers don't have to call FlushAsync explicitly when they don't need a
                // mid-delegate read-after-write. A no-op if everything was already flushed.
                await dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return result;
            },
            verifySucceeded: null,
            cancellationToken: cancellationToken
        );
    }
}
