using HumbleMediator;
using WebApiTemplate.Core.Common;
using WebApiTemplate.Core.Customers;

namespace WebApiTemplate.Application.Customers.Queries;

/// <summary>
/// Handles listing customers with search, sort, and pagination.
/// </summary>
/// <param name="readRepository">The customer read repository.</param>
public class ListCustomersQueryHandler(ICustomerReadRepository readRepository)
    : IQueryHandler<ListCustomersQuery, PagedResult<Customer>>
{
    /// <inheritdoc />
    public Task<PagedResult<Customer>> Handle(
        ListCustomersQuery query,
        CancellationToken cancellationToken = default
    ) => readRepository.ListAsync(query.Search, query.OrderBy, query.Offset, query.Limit);
}
