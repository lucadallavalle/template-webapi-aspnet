using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApiTemplate.Infrastructure.Persistence;

/// <summary>
/// Creates a repository instance bound to a specific <see cref="DbContext"/>, so the repository's
/// staged changes participate in the unit of work's transaction.
/// </summary>
/// <typeparam name="TRepository">The repository contract to create.</typeparam>
/// <param name="serviceProvider">The provider used to discover the concrete repository type and its dependencies.</param>
public sealed class RepositoryFactory<TRepository>(IServiceProvider serviceProvider)
    where TRepository : notnull
{
    /// <summary>
    /// Creates a <typeparamref name="TRepository"/> bound to <paramref name="dbContext"/>.
    /// </summary>
    /// <typeparam name="TDbContext">The database context type.</typeparam>
    /// <param name="dbContext">The context the created repository must use.</param>
    /// <returns>A repository instance bound to <paramref name="dbContext"/>.</returns>
    public TRepository Create<TDbContext>(TDbContext dbContext)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var repositoryType = serviceProvider.GetRequiredService<TRepository>().GetType();
        return (TRepository)
            ActivatorUtilities.CreateInstance(serviceProvider, repositoryType, dbContext);
    }
}
