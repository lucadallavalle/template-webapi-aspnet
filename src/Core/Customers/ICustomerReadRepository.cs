using WebApiTemplate.Core.Persistence;

namespace WebApiTemplate.Core.Customers;

/// <summary>
/// Read repository contract for <see cref="Customer"/> entities. Injected directly into query handlers.
/// </summary>
public interface ICustomerReadRepository : IReadRepository<Customer>;
