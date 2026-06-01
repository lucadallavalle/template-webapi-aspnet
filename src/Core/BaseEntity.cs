namespace WebApiTemplate.Core;

/// <summary>
/// Base class for all domain entities.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets or sets the id of the entity. Null until the entity has been persisted.
    /// </summary>
    public int? Id { get; set; }
}
