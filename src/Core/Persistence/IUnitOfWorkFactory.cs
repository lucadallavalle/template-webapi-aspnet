namespace WebApiTemplate.Core.Persistence;

/// <summary>
/// Factory for executing a unit of work inside a retriable database transaction.
/// </summary>
/// <remarks>
/// The factory owns the lifetime of the underlying database context and the transaction.
/// The provided delegate is the atomic, replayable unit: it may be re-executed by the
/// configured execution strategy if a transient failure occurs. Therefore the delegate
/// must be idempotent with respect to in-memory state and must not perform non-idempotent
/// side effects (HTTP calls, queue publishes, etc.) that would be unsafe to repeat.
/// </remarks>
public interface IUnitOfWorkFactory
{
    /// <summary>
    /// Executes <paramref name="work"/> inside a database transaction managed by the
    /// configured execution strategy. The transaction is committed on successful return
    /// and rolled back if <paramref name="work"/> throws. Transient failures cause the
    /// entire delegate to be retried with a fresh database context.
    /// </summary>
    /// <param name="work">The unit of work to execute.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
    Task ExecuteInTransactionAsync(
        Func<IUnitOfWork, CancellationToken, Task> work,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Executes <paramref name="work"/> inside a database transaction managed by the
    /// configured execution strategy and returns its result. The transaction is committed
    /// on successful return and rolled back if <paramref name="work"/> throws. Transient
    /// failures cause the entire delegate to be retried with a fresh database context.
    /// </summary>
    /// <typeparam name="TResult">The type of the value produced by <paramref name="work"/>.</typeparam>
    /// <param name="work">The unit of work to execute.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>The value produced by <paramref name="work"/>.</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<IUnitOfWork, CancellationToken, Task<TResult>> work,
        CancellationToken cancellationToken = default
    );
}
