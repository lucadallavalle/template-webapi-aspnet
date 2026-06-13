using HumbleMediator;
using WebApiTemplate.Core.Common;

namespace WebApiTemplate.Application.Customers.Queries;

/// <summary>
/// Query to list customers with search, sorting, and pagination.
/// </summary>
/// <param name="Search">Free-text search term.</param>
/// <param name="OrderBy">Comma-separated sort fields; prefix with <c>-</c> for descending.</param>
/// <param name="Offset">Zero-based offset.</param>
/// <param name="Limit">Maximum number of items to return.</param>
public sealed record ListCustomersQuery(
    string? Search,
    string? OrderBy,
    int Offset = 0,
    int Limit = 25
) : IQuery<PagedResult<CustomerDto>>;
