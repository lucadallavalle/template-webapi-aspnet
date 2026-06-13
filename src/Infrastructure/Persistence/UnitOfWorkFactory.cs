using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
        Func<CancellationToken, Task<bool>>? verifySucceeded = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(work);

        // Adapt the void verifier to the result-bearing one: there is no value to recover, so a
        // confirmed commit simply carries null.
        Func<CancellationToken, Task<CommitVerification<object?>>>? verify = verifySucceeded is null
            ? null
            : async ct => new CommitVerification<object?>(await verifySucceeded(ct), Result: null);

        return ExecuteInTransactionAsync<object?>(
            async (uow, ct) =>
            {
                await work(uow, ct);
                return null;
            },
            verify,
            cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<IUnitOfWork, CancellationToken, Task<TResult>> work,
        Func<CancellationToken, Task<CommitVerification<TResult>>>? verifySucceeded = null,
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
            state: (
                Factory: dbContextFactory,
                ServiceProvider: serviceProvider,
                Work: work,
                Verify: verifySucceeded
            ),
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
            // Called by the strategy only when a transient failure leaves commit success ambiguous.
            // Without a caller-supplied verifier we cannot prove the commit landed, so we report
            // "not succeeded" — behaviourally identical to passing null (the strategy retries),
            // i.e. at-least-once. A supplied verifier can instead confirm the commit and short-circuit.
            verifySucceeded: static async (state, ct) =>
            {
                if (state.Verify is null)
                {
                    return new ExecutionResult<TResult>(successful: false, result: default!);
                }

                var verification = await state.Verify(ct);
                return new ExecutionResult<TResult>(verification.IsCommitted, verification.Result);
            },
            cancellationToken: cancellationToken
        );
    }
}
