using HumbleMediator;

namespace WebApiTemplate.Application.Customers.Queries;

/// <summary>
/// Query to get a customer by its id.
/// </summary>
/// <param name="Id">The id of the customer to get.</param>
public sealed record GetCustomerByIdQuery(int Id) : IQuery<CustomerDto?>;
