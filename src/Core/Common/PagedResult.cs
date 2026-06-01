namespace WebApiTemplate.Core.Common;

/// <summary>
/// Generic paginated result containing the page of items and the total row count.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);
