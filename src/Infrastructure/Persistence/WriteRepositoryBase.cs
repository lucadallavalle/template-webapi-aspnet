using Microsoft.EntityFrameworkCore;
using WebApiTemplate.Core;
using WebApiTemplate.Core.Persistence;

namespace WebApiTemplate.Infrastructure.Persistence;

/// <summary>
/// Base Entity Framework Core write repository, bound to the unit of work's <see cref="DbContext"/>.
/// </summary>
/// <remarks>
/// Mutating methods (<see cref="AddAsync"/>, <see cref="UpdateAsync"/>, <see cref="DeleteByIdAsync"/>) only
/// stage changes in the change tracker. They do not emit SQL on their own. Persistence is performed atomically
/// by the surrounding
/// <see cref="IUnitOfWorkFactory.ExecuteInTransactionAsync(System.Func{IUnitOfWork, System.Threading.CancellationToken, System.Threading.Tasks.Task}, System.Threading.CancellationToken)"/>
/// scope, which flushes and commits once the delegate returns. If a later operation in the same delegate
/// needs a store-generated value (e.g. an identity key), call <see cref="IUnitOfWork.FlushAsync"/> to
/// materialise pending changes mid-delegate.
/// </remarks>
/// <typeparam name="TEntity">The entity type the repository manages.</typeparam>
/// <typeparam name="TKey">The type of the entity's identifier.</typeparam>
/// <typeparam name="TContext">The database context type.</typeparam>
/// <param name="context">The database context this repository is bound to.</param>
public abstract class WriteRepositoryBase<TEntity, TKey, TContext>(TContext context)
    : IWriteRepository<TEntity, TKey>
    where TContext : DbContext
    where TEntity : BaseEntity
{
    /// <summary>
    /// The database context backing this repository, available to subclasses for custom operations.
    /// </summary>
    protected TContext Context { get; } = context;

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetByIdAsync(
        TKey id,
        CancellationToken cancellationToken = default
    )
    {
        if (id is null)
        {
            return null;
        }

        // FindAsync resolves the entity by primary key and returns a change-tracked instance, which the
        // load-then-modify update flow relies on (mutating it emits an UPDATE on commit).
        return await Context.Set<TEntity>().FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default
    )
    {
        await Context.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Guard the EF "silent insert" footgun: calling Update on an entity whose key is unset makes EF
        // treat it as Added and emit an INSERT instead of an UPDATE. Prefer load-then-modify (load via
        // GetByIdAsync, mutate the tracked entity); this guard makes a detached, key-less entity fail loud.
        if (entity.Id is null)
        {
            throw new InvalidOperationException(
                $"Cannot update a {typeof(TEntity).Name} with no Id. Load the entity first, then modify the tracked instance."
            );
        }

        Context.Set<TEntity>().Update(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual async Task DeleteByIdAsync(
        TKey id,
        CancellationToken cancellationToken = default
    )
    {
        var entity = await GetByIdAsync(id, cancellationToken);

        if (entity is null)
        {
            return;
        }

        Context.Set<TEntity>().Remove(entity);
    }
}
