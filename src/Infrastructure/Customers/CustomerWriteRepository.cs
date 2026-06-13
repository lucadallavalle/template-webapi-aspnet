using WebApiTemplate.Core.Customers;
using WebApiTemplate.Infrastructure.Persistence;

namespace WebApiTemplate.Infrastructure.Customers;

/// <summary>
/// Write repository for <see cref="Customer"/> entities. Resolved inside the unit of work and bound to
/// its transactional <see cref="AppDbContext"/>.
/// </summary>
/// <param name="context">The database context bound to the current unit of work.</param>
public sealed class CustomerWriteRepository(AppDbContext context)
    : WriteRepositoryBase<Customer, int, AppDbContext>(context),
        ICustomerWriteRepository;
