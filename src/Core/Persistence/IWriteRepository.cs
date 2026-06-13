namespace WebApiTemplate.Core.Persistence;

/// <summary>
/// Base contract for write repositories. Resolved <b>inside</b> a unit-of-work transaction via
/// <see cref="IUnitOfWork.GetRepository{TRepository}"/>. Mutating methods only stage changes in the
/// underlying change tracker; the unit of work flushes and commits.
/// </summary>
/// <remarks>
/// <see cref="GetByIdAsync"/> returns a <i>change-tracked</i> entity so commands can load-then-modify
/// within the transaction (the modified entity is emitted as an UPDATE on commit). This is distinct
/// from <see cref="IReadRepository{T}.GetById"/>, which is a stateless, no-tracking query read.
/// </remarks>
/// <typeparam name="TEntity">The entity type the repository manages.</typeparam>
/// <typeparam name="TKey">The type of the entity's identifier.</typeparam>
public interface IWriteRepository<TEntity, in TKey>
    where TEntity : class
{
    /// <summary>
    /// Gets a change-tracked entity by its identifier, for load-then-modify within the transaction.
    /// </summary>
    /// <param name="id">The identifier of the entity to get.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>The tracked entity, if found; otherwise <see langword="null"/>.</returns>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new entity for insertion.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages an existing entity for update.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages the entity with the given identifier for deletion, if it exists.
    /// </summary>
    /// <param name="id">The identifier of the entity to delete.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
    Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default);
}
