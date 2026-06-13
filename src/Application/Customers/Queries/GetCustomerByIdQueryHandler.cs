using HumbleMediator;
using WebApiTemplate.Core.Customers;

namespace WebApiTemplate.Application.Customers.Queries;

/// <summary>
/// Handles <see cref="GetCustomerByIdQuery"/>. Reads run outside any unit of work, straight through
/// the read repository.
/// </summary>
/// <param name="customers">The customer read repository.</param>
public sealed class GetCustomerByIdQueryHandler(ICustomerReadRepository customers)
    : IQueryHandler<GetCustomerByIdQuery, CustomerDto?>
{
    /// <summary>
    /// Handles the <see cref="GetCustomerByIdQuery"/>.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken" />.</param>
    /// <returns>The customer DTO, if found; otherwise <see langword="null"/>.</returns>
    public async Task<CustomerDto?> Handle(
        GetCustomerByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var customer = await customers.GetById(query.Id, cancellationToken);
        return customer is null ? null : new CustomerDto(customer.Id!.Value);
    }
}
