using WebApiTemplate.Core.Persistence;

namespace WebApiTemplate.Core.Customers;

/// <summary>
/// Write repository contract for <see cref="Customer"/> entities. Resolved inside a unit-of-work
/// transaction via <see cref="IUnitOfWork.GetRepository{TRepository}"/>.
/// </summary>
public interface ICustomerWriteRepository : IWriteRepository<Customer, int>;
