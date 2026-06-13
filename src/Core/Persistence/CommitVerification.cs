namespace WebApiTemplate.Core.Persistence;

/// <summary>
/// The outcome of a post-failure commit verification. When a transient failure leaves it ambiguous
/// whether a transaction's commit actually reached the database, a caller-supplied verifier returns
/// this to tell the execution strategy whether to stop (the commit landed) or retry.
/// </summary>
/// <remarks>
/// This is the persistence-agnostic counterpart to EF Core's <c>ExecutionResult&lt;T&gt;</c>; it is
/// intentionally defined here so <c>Core</c>/<c>Application</c> can describe exactly-once semantics
/// without taking a dependency on EF Core.
/// </remarks>
/// <typeparam name="TResult">The type of the value the unit of work produces.</typeparam>
/// <param name="IsCommitted">
/// <see langword="true"/> if the prior attempt's transaction is confirmed committed (stop retrying and
/// return <paramref name="Result"/>); otherwise <see langword="false"/> (retry).
/// </param>
/// <param name="Result">The value to return when <paramref name="IsCommitted"/> is <see langword="true"/>.</param>
public readonly record struct CommitVerification<TResult>(bool IsCommitted, TResult Result)
{
    /// <summary>
    /// Creates a verification result indicating the transaction committed, carrying the value to return.
    /// </summary>
    /// <param name="result">The value to return without retrying.</param>
    /// <returns>A committed verification result.</returns>
    public static CommitVerification<TResult> Committed(TResult result) => new(true, result);

    /// <summary>
    /// A verification result indicating the transaction did not commit, so the execution strategy retries.
    /// </summary>
    public static CommitVerification<TResult> NotCommitted { get; } = new(false, default!);
}
