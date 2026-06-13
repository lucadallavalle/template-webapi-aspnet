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
/// <para>
/// Replay makes the default behaviour <b>at-least-once</b>: if a transient failure occurs after the
/// commit was sent but before its acknowledgement arrives, the strategy cannot tell the commit landed
/// and replays the whole delegate — which can duplicate effects (e.g. a second INSERT). For
/// <b>exactly-once</b> semantics, pass a <c>verifySucceeded</c> callback that reports whether the prior
/// attempt actually committed. The standard implementation writes a unique token row inside the
/// transaction and checks for its presence in the callback; see the README for the full recipe. Run that
/// verification query on a retry-enabled context (e.g. one from the same <c>IDbContextFactory</c>), since
/// the connection that failed during commit is likely to fail again during verification.
/// </para>
/// <para>
/// See EF Core's connection-resiliency guidance:
/// https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency#transaction-commit-failure-and-the-idempotency-issue
/// </para>
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
    /// <param name="verifySucceeded">
    /// Optional commit verifier, invoked only after a transient failure leaves commit success ambiguous.
    /// It returns <see langword="true"/> if the prior attempt's transaction is confirmed committed (stop
    /// retrying); otherwise <see langword="false"/> (retry). When <see langword="null"/> the strategy
    /// always retries (at-least-once).
    /// </param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
    Task ExecuteInTransactionAsync(
        Func<IUnitOfWork, CancellationToken, Task> work,
        Func<CancellationToken, Task<bool>>? verifySucceeded = null,
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
    /// <param name="verifySucceeded">
    /// Optional commit verifier, invoked only after a transient failure leaves commit success ambiguous.
    /// It returns a <see cref="CommitVerification{TResult}"/> stating whether the prior attempt's
    /// transaction committed and, if so, the result to return without retrying. When <see langword="null"/>
    /// the strategy always retries (at-least-once).
    /// </param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>The value produced by <paramref name="work"/>.</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<IUnitOfWork, CancellationToken, Task<TResult>> work,
        Func<CancellationToken, Task<CommitVerification<TResult>>>? verifySucceeded = null,
        CancellationToken cancellationToken = default
    );
}
