using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebApiTemplate.Core.Customers;
using WebApiTemplate.Infrastructure.Persistence;

namespace WebApiTemplate.Infrastructure.Customers;

/// <summary>
/// Repository for reading <see cref="Customer"/> entities. Supplies the entity-specific
/// search and sort behaviour on top of <see cref="ReadRepositoryBase{T}"/>.
/// </summary>
/// <param name="dbContextFactory">The DbContext factory.</param>
public class CustomerReadRepository(IDbContextFactory<AppDbContext> dbContextFactory)
    : ReadRepositoryBase<Customer>(dbContextFactory),
        ICustomerReadRepository
{
    // Customer only carries an Id in this template. For a real entity, override ApplySearch
    // to declare which fields are searchable, e.g.:
    //
    //   protected override IQueryable<Customer> ApplySearch(IQueryable<Customer> query, string search)
    //   {
    //       var pattern = $"%{search}%";
    //       return query.Where(c => EF.Functions.ILike(c.Name, pattern));
    //   }

    /// <inheritdoc />
    protected override Expression<Func<Customer, object?>>? GetSortSelector(string fieldName) =>
        fieldName.ToLowerInvariant() switch
        {
            "id" => c => c.Id,
            _ => null,
        };
}
