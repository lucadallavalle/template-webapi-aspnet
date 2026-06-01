namespace WebApiTemplate.WebApi.Common;

/// <summary>
/// API envelope for a page of results: the items plus paging metadata.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The page of items.</param>
/// <param name="Total">The total number of matching rows.</param>
/// <param name="Offset">The zero-based offset of this page.</param>
/// <param name="Limit">The requested page size.</param>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Total, int Offset, int Limit);
