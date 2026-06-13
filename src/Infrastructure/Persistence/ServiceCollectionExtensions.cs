using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApiTemplate.Core.Persistence;

namespace WebApiTemplate.Infrastructure.Persistence;

/// <summary>
/// Service-collection extensions that wire up the persistence stack.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the full persistence stack for <typeparamref name="T"/>: the write-side unit-of-work
    /// stack (see <see cref="AddWriteRepositories{T}"/>) plus the read repositories (see
    /// <see cref="AddReadRepositories{T}"/>).
    /// </summary>
    /// <remarks>
    /// Read repositories are registered in the service collection here, but because query handlers are
    /// resolved by the primary (SimpleInjector) container they must also be cross-wired into it from
    /// the composition root.
    /// </remarks>
    /// <typeparam name="T">The application's <see cref="DbContext"/> type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsAction">Configures the <see cref="DbContextOptionsBuilder"/> for <typeparamref name="T"/>.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddPersistence<T>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> optionsAction
    )
        where T : DbContext =>
        services.AddWriteRepositories<T>(optionsAction).AddReadRepositories<T>();

    /// <summary>
    /// Registers the write-side unit-of-work stack for <typeparamref name="T"/>: a scoped
    /// <see cref="IDbContextFactory{TContext}"/>, the <see cref="IUnitOfWorkFactory"/>, and every
    /// concrete write repository (a class deriving from <see cref="WriteRepositoryBase{TEntity, TKey, TContext}"/>)
    /// discovered in <typeparamref name="T"/>'s assembly, together with its repository factory.
    /// </summary>
    /// <typeparam name="T">The application's <see cref="DbContext"/> type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsAction">Configures the <see cref="DbContextOptionsBuilder"/> for <typeparamref name="T"/>.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddWriteRepositories<T>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> optionsAction
    )
        where T : DbContext
    {
        // The factory is registered Scoped (not the default Singleton/pooled): the options action
        // resolves request-scoped services (e.g. the NpgsqlDataSource registration), which a
        // root-resolved singleton factory cannot do without tripping scope validation.
        services.AddDbContextFactory<T>(optionsAction, ServiceLifetime.Scoped);
        services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory<T>>();

        // Each concrete write repository is registered against its entity-specific interface, along
        // with the RepositoryFactory the UnitOfWork uses to resolve it inside the transaction.
        foreach (var (repository, contract) in ConcreteRepositories<T>("WriteRepositoryBase`3"))
        {
            services.AddScoped(contract, repository);
            services.AddScoped(typeof(RepositoryFactory<>).MakeGenericType(contract));
        }

        return services;
    }

    /// <summary>
    /// Registers every read repository (a class deriving from <see cref="ReadRepositoryBase{T}"/>)
    /// discovered in <typeparamref name="T"/>'s assembly against its entity-specific interface. Read
    /// repositories are not unit-of-work bound — they are injected directly into query handlers.
    /// </summary>
    /// <typeparam name="T">The application's <see cref="DbContext"/> type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddReadRepositories<T>(this IServiceCollection services)
        where T : DbContext
    {
        foreach (var (repository, contract) in ConcreteRepositories<T>("ReadRepositoryBase`1"))
        {
            services.AddScoped(contract, repository);
        }

        return services;
    }

    // Finds concrete repository classes in T's assembly whose immediate base type matches
    // baseTypeName, paired with their entity-specific (non-generic) repository interface.
    private static IEnumerable<(Type Repository, Type Contract)> ConcreteRepositories<T>(
        string baseTypeName
    )
        where T : DbContext =>
        typeof(T)
            .Assembly.GetTypes()
            .Where(t =>
                t.IsClass
                && !t.IsAbstract
                && t.Name.EndsWith("Repository", StringComparison.Ordinal)
                && (
                    t.BaseType?.Name.Equals(baseTypeName, StringComparison.OrdinalIgnoreCase)
                    ?? false
                )
            )
            .SelectMany(t =>
                t.GetInterfaces()
                    // Take only the entity-specific repository interface (e.g. ICustomerWriteRepository),
                    // not the generic base contracts (IWriteRepository<,> / IReadRepository<>).
                    .Where(i =>
                        !i.IsGenericType && i.Name.EndsWith("Repository", StringComparison.Ordinal)
                    )
                    .Select(i => (Repository: t, Contract: i))
            );
}
