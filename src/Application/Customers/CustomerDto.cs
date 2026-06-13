namespace WebApiTemplate.Application.Customers;

/// <summary>
/// Data transfer object for a customer, returned by the application layer to the presentation
/// layer so the domain entity is never exposed across the API boundary.
/// </summary>
/// <param name="Id">The customer's identifier.</param>
public sealed record CustomerDto(int Id);
