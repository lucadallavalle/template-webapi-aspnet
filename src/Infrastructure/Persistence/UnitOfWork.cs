using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApiTemplate.Core.Persistence;

namespace WebApiTemplate.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core implementation of <see cref="IUnitOfWork"/>.
/// </summary>
/// <remarks>
/// Instances are created by <see cref="UnitOfWorkFactory{TDbContext}"/> for the lifetime of a single
/// <see cref="IUnitOfWorkFactory.ExecuteInTransactionAsync(System.Func{IUnitOfWork, System.Threading.CancellationToken, System.Threading.Tasks.Task}, System.Threading.CancellationToken)"/>
/// invocation. The unit of work itself owns no disposable resources: the underlying
/// <see cref="DbContext"/> and transaction are owned by the factory and disposed at the
/// end of the delegate's execution (including across retry attempts).
/// </remarks>
/// <typeparam name="TDbContext">The database context type.</typeparam>
/// <param name="dbContext">The database context bound to the current transaction.</param>
/// <param name="serviceProvider">The provider used to resolve repositories bound to <paramref name="dbContext"/>.</param>
public sealed class UnitOfWork<TDbContext>(TDbContext dbContext, IServiceProvider serviceProvider)
    : IUnitOfWork
    where TDbContext : DbContext
{
    private TDbContext DbContext { get; } = dbContext;

    /// <inheritdoc />
    public TRepository GetRepository<TRepository>()
        where TRepository : class
    {
        var factory = serviceProvider.GetRequiredService<RepositoryFactory<TRepository>>();
        return factory.Create(DbContext);
    }

    /// <inheritdoc />
    public Task FlushAsync(CancellationToken cancellationToken = default) =>
        DbContext.SaveChangesAsync(cancellationToken);
}
