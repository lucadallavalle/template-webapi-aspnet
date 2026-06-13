namespace WebApiTemplate.Core.Persistence;

/// <summary>
/// Represents a unit of work scope inside which one or more repositories
/// can collaborate within a single, atomic, retriable database transaction.
/// </summary>
/// <remarks>
/// Instances are produced and lifetime-managed by an <see cref="IUnitOfWorkFactory"/>
/// and are only valid inside the delegate passed to
/// <see cref="IUnitOfWorkFactory.ExecuteInTransactionAsync(System.Func{IUnitOfWork, System.Threading.CancellationToken, System.Threading.Tasks.Task}, System.Func{System.Threading.CancellationToken, System.Threading.Tasks.Task{System.Boolean}}, System.Threading.CancellationToken)"/>.
/// Do not store references outside that scope.
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Resolves a repository bound to the unit of work's underlying database context,
    /// so changes staged through the repository participate in the same transaction.
    /// </summary>
    /// <typeparam name="TRepository">The repository contract to resolve.</typeparam>
    /// <returns>The repository instance bound to this unit of work.</returns>
    TRepository GetRepository<TRepository>()
        where TRepository : class;

    /// <summary>
    /// Flushes currently staged changes to the database without ending the transaction.
    /// </summary>
    /// <remarks>
    /// Repositories typically only stage changes (e.g. <c>Add</c> / <c>Update</c> / <c>Remove</c>) without
    /// emitting SQL. Calling this method materialises those staged changes inside the open transaction,
    /// which is required when subsequent operations in the same delegate depend on store-generated
    /// values (e.g. identity keys) produced by earlier operations. A final flush is performed
    /// automatically by the factory before the transaction is committed, so calling this method is
    /// only necessary for read-after-write scenarios within the delegate.
    /// </remarks>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
