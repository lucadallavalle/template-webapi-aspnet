using HumbleMediator;
using WebApiTemplate.Core.Common;
using WebApiTemplate.Core.Customers;

namespace WebApiTemplate.Application.Customers.Queries;

/// <summary>
/// Handles listing customers with search, sort, and pagination. Reads run outside any unit of work,
/// straight through the read repository.
/// </summary>
/// <param name="customers">The customer read repository.</param>
public sealed class ListCustomersQueryHandler(ICustomerReadRepository customers)
    : IQueryHandler<ListCustomersQuery, PagedResult<CustomerDto>>
{
    /// <summary>
    /// Handles the <see cref="ListCustomersQuery"/>.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken" />.</param>
    /// <returns>A page of customer DTOs and the total count.</returns>
    public async Task<PagedResult<CustomerDto>> Handle(
        ListCustomersQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var page = await customers.ListAsync(
            query.Search,
            query.OrderBy,
            query.Offset,
            query.Limit,
            cancellationToken
        );

        var items = page.Items.Select(c => new CustomerDto(c.Id!.Value)).ToList();
        return new PagedResult<CustomerDto>(items, page.Total);
    }
}
