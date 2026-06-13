using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebApiTemplate.Core;
using WebApiTemplate.Core.Common;
using WebApiTemplate.Core.Persistence;

namespace WebApiTemplate.Infrastructure.Persistence;

/// <summary>
/// Base class for read repositories, backed by EF Core via <see cref="IDbContextFactory{TContext}"/>.
/// Each call opens its own short-lived, no-tracking context — there is no transaction and no unit of
/// work — so reads are cheap and could be routed to a read replica by pointing the factory's
/// connection at one. Provides pagination/sorting/search boilerplate; subclasses supply entity-specific
/// search predicates and sort-field mappings by overriding <see cref="ApplySearch"/>,
/// <see cref="GetSortSelector"/>, and <see cref="ApplyDefaultSort"/>.
/// </summary>
/// <param name="dbContextFactory">The DbContext factory.</param>
/// <typeparam name="T">The entity type.</typeparam>
public abstract class ReadRepositoryBase<T>(IDbContextFactory<AppDbContext> dbContextFactory)
    : IReadRepository<T>
    where T : BaseEntity
{
    /// <summary>
    /// The DbContext factory, available to subclasses for custom queries.
    /// </summary>
    protected IDbContextFactory<AppDbContext> DbContextFactory { get; } = dbContextFactory;

    /// <inheritdoc />
    public virtual async Task<T?> GetById(int id, CancellationToken cancellationToken = default)
    {
        await using var ctx = await DbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ctx.Set<T>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<PagedResult<T>> ListAsync(
        string? search,
        string? orderBy,
        int offset,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        await using var ctx = await DbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = ctx.Set<T>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = ApplySearch(query, search);
        }

        var total = await query.CountAsync(cancellationToken);

        query = ApplySorting(query, orderBy);

        var items = await query.Skip(offset).Take(limit).ToListAsync(cancellationToken);

        return new PagedResult<T>(items, total);
    }

    /// <summary>
    /// Applies free-text search filtering. Subclasses override to define which fields are
    /// searchable for their entity. The default returns the query unchanged (no search).
    /// </summary>
    protected virtual IQueryable<T> ApplySearch(IQueryable<T> query, string search) => query;

    /// <summary>
    /// Returns the sort selector for a given field name. Subclasses override to define which
    /// fields are sortable. Returns <c>null</c> for unrecognised fields (they are skipped).
    /// </summary>
    protected virtual Expression<Func<T, object?>>? GetSortSelector(string fieldName) => null;

    /// <summary>
    /// Returns the default sort applied when no <c>orderBy</c> is specified. Defaults to
    /// ascending by Id; subclasses may override.
    /// </summary>
    protected virtual IOrderedQueryable<T> ApplyDefaultSort(IQueryable<T> query) =>
        query.OrderBy(e => e.Id);

    private IQueryable<T> ApplySorting(IQueryable<T> query, string? orderBy)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
        {
            return ApplyDefaultSort(query);
        }

        var fields = orderBy.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        IOrderedQueryable<T>? orderedQuery = null;

        foreach (var field in fields)
        {
            var descending = field.StartsWith('-');
            var fieldName = descending ? field[1..] : field;

            var selector = GetSortSelector(fieldName);
            if (selector is null)
            {
                continue;
            }

            if (orderedQuery is null)
            {
                orderedQuery = descending
                    ? query.OrderByDescending(selector)
                    : query.OrderBy(selector);
            }
            else
            {
                orderedQuery = descending
                    ? orderedQuery.ThenByDescending(selector)
                    : orderedQuery.ThenBy(selector);
            }
        }

        return orderedQuery ?? ApplyDefaultSort(query);
    }
}
