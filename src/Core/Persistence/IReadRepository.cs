using WebApiTemplate.Core.Common;

namespace WebApiTemplate.Core.Persistence;

/// <summary>
/// Base contract for read repositories. Reads run <b>outside</b> any unit of work, on a fresh
/// no-tracking context, so they carry no transaction overhead and can later be pointed at a read
/// replica without touching callers. Inject these directly into query handlers.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IReadRepository<T>
    where T : BaseEntity
{
    /// <summary>
    /// Gets an entity by its id (no change tracking).
    /// </summary>
    /// <param name="id">The id of the entity to get.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>The entity, if found; otherwise <see langword="null"/>.</returns>
    Task<T?> GetById(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists entities with free-text search, sorting, and pagination. Results are materialised
    /// (no <see cref="IQueryable{T}"/> is exposed to callers).
    /// </summary>
    /// <param name="search">Free-text search term (entity-specific fields).</param>
    /// <param name="orderBy">Comma-separated sort fields; prefix a field with <c>-</c> for descending.</param>
    /// <param name="offset">Zero-based offset.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>A paged result with the items and the total count.</returns>
    Task<PagedResult<T>> ListAsync(
        string? search,
        string? orderBy,
        int offset,
        int limit,
        CancellationToken cancellationToken = default
    );
}
