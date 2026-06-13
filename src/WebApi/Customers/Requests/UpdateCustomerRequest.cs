using WebApiTemplate.Core.Customers;

namespace WebApiTemplate.WebApi.Customers.Requests;

/// <summary>
/// Request entity for updating a customer.
/// </summary>
public class UpdateCustomerRequest
{
    /// <summary>
    /// Maps this request to a domain entity. An instance method so the bound request body is the
    /// source of the mapping. The id is NOT carried here — the handler applies the route id to the
    /// loaded entity, so the URL is the single source of truth for which row is updated.
    /// </summary>
    /// <returns>The domain entity.</returns>
    public Customer ToDomainEntity() => new();
}
