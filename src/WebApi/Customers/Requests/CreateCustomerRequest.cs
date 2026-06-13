using WebApiTemplate.Core.Customers;

namespace WebApiTemplate.WebApi.Customers.Requests;

/// <summary>
/// Request entity for creating a customer.
/// </summary>
public class CreateCustomerRequest
{
    /// <summary>
    /// Maps this request to a domain entity. An instance method so the bound request body is the
    /// source of the mapping (a real request copies its fields onto the new <see cref="Customer"/>).
    /// </summary>
    /// <returns>The domain entity.</returns>
    public Customer ToDomainEntity() => new();
}
