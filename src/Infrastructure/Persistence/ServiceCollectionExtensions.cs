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
    /// the composition root. Use <see cref="ReadRepositoryContracts{T}"/> to enumerate the contracts to
    /// cross-wire so new read repositories are picked up automatically.
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
    /// <see cref="IDbContextFactory{TContext}"/>, the <see cref="IUnitOfWorkFactory"/>, and a repository
    /// factory for every concrete write repository (a class deriving from
    /// <see cref="WriteRepositoryBase{TEntity, TKey, TContext}"/>) discovered in <typeparamref name="T"/>'s assembly.
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

        // Write repositories are never injected directly: the unit of work creates them via
        // RepositoryFactory<TContract>, bound to the transaction's DbContext. Register each factory
        // with its concrete implementation type so it can ActivatorUtilities-create the repository
        // directly — no need to resolve (and immediately discard) a throwaway instance just to learn
        // its type, which would also force a stray DbContext to be constructed.
        foreach (
            var (implementation, contract) in ConcreteRepositories<T>(
                typeof(WriteRepositoryBase<,,>)
            )
        )
        {
            var factoryType = typeof(RepositoryFactory<>).MakeGenericType(contract);
            services.AddScoped(
                factoryType,
                sp => ActivatorUtilities.CreateInstance(sp, factoryType, implementation)
            );
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
        foreach (
            var (implementation, contract) in ConcreteRepositories<T>(typeof(ReadRepositoryBase<>))
        )
        {
            services.AddScoped(contract, implementation);
        }

        return services;
    }

    /// <summary>
    /// Enumerates the entity-specific read-repository contracts (e.g. <c>ICustomerReadRepository</c>)
    /// discovered in <typeparamref name="T"/>'s assembly. The composition root cross-wires these into
    /// SimpleInjector so query handlers can resolve them, automatically picking up new read repositories.
    /// </summary>
    /// <typeparam name="T">The application's <see cref="DbContext"/> type.</typeparam>
    /// <returns>The distinct read-repository contract types.</returns>
    public static IEnumerable<Type> ReadRepositoryContracts<T>()
        where T : DbContext =>
        ConcreteRepositories<T>(typeof(ReadRepositoryBase<>)).Select(x => x.Contract).Distinct();

    // Finds concrete repository classes in T's assembly that derive (at any depth) from the given open
    // generic base (e.g. WriteRepositoryBase<,,>), paired with their entity-specific (non-generic)
    // repository interface.
    private static IEnumerable<(Type Implementation, Type Contract)> ConcreteRepositories<T>(
        Type openGenericBase
    )
        where T : DbContext =>
        typeof(T)
            .Assembly.GetTypes()
            .Where(t =>
                t.IsClass
                && !t.IsAbstract
                && t.Name.EndsWith("Repository", StringComparison.Ordinal)
                && DerivesFromGeneric(t, openGenericBase)
            )
            .SelectMany(t =>
                t.GetInterfaces()
                    // Take only the entity-specific repository interface (e.g. ICustomerWriteRepository),
                    // not the generic base contracts (IWriteRepository<,> / IReadRepository<>).
                    .Where(i =>
                        !i.IsGenericType && i.Name.EndsWith("Repository", StringComparison.Ordinal)
                    )
                    .Select(i => (Implementation: t, Contract: i))
            );

    // Walks the base-type chain looking for a constructed generic whose definition is openGenericBase.
    // Matching the generic type definition (rather than a mangled type name) is robust to intermediate
    // abstract bases and independent of naming.
    private static bool DerivesFromGeneric(Type type, Type openGenericBase)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == openGenericBase)
            {
                return true;
            }
        }

        return false;
    }
}
