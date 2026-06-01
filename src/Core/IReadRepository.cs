using WebApiTemplate.Core.Common;

namespace WebApiTemplate.Core;

/// <summary>
/// Base interface for read repositories.
/// </summary>
/// <typeparam name="T">The domain entity type, must inherit from BaseEntity.</typeparam>
public interface IReadRepository<T>
    where T : BaseEntity
{
    /// <summary>
    /// Gets the entity by its id. When <paramref name="uow"/> is provided the entity is read
    /// through its DbContext (so it is change-tracked); otherwise a fresh, stateless DbContext
    /// is used.
    /// </summary>
    /// <param name="id">The id of the entity to get.</param>
    /// <param name="uow">Optional unit of work for tracked reads.</param>
    /// <returns>The entity, if found, otherwise null.</returns>
    public Task<T?> GetById(int id, IUnitOfWork? uow = null);

    /// <summary>
    /// Lists entities with free-text search, sorting, and pagination.
    /// </summary>
    /// <param name="search">Free-text search term (entity-specific fields).</param>
    /// <param name="orderBy">Comma-separated sort fields; prefix a field with <c>-</c> for descending.</param>
    /// <param name="offset">Zero-based offset.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <returns>A paged result with the items and the total count.</returns>
    public Task<PagedResult<T>> ListAsync(string? search, string? orderBy, int offset, int limit);
}
