// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable CS8618

using System.Data.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;
using WebApiTemplate.Infrastructure.Persistence;
using Xunit;

namespace WebApiTemplate.IntegrationTests;

public class AppWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private Respawner _respawner;

    public AppWebApplicationFactory()
    {
        _dbContainer = new PostgreSqlBuilder("postgres:18")
            .WithDatabase(Constants.TestPostgresDatabase)
            .WithUsername(Constants.TestPostgresUsername)
            .WithPassword(Constants.TestPostgresPassword)
            .WithExposedPort(Constants.TestPostgresPort)
            .WithPortBinding(Constants.TestPostgresPort)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Force host startup so Program.cs runs MigrateAsync in Development mode.
        _ = this.Services;

        // Resolve the Respawn connection from the singleton NpgsqlDataSource — the
        // DbContext and its factory are Scoped and cannot be resolved from the root
        // provider here.
        await using var connection = this
            .Services.GetRequiredService<NpgsqlDataSource>()
            .CreateConnection();
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                TablesToIgnore = new Table[] { "__EFMigrationsHistory" },
            }
        );
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var typesToRemove = new[]
            {
                typeof(DbContextOptions),
                typeof(DbContextOptions<AppDbContext>),
                typeof(IDbContextFactory<AppDbContext>),
                typeof(NpgsqlDataSource),
                typeof(DbConnection),
                typeof(DbDataSource),
                typeof(NpgsqlConnection),
            };

            var toRemove = services.Where(e => typesToRemove.Contains(e.ServiceType)).ToList();
            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            services.AddNpgsqlDataSource(_dbContainer.GetConnectionString());

            Action<IServiceProvider, DbContextOptionsBuilder> dbConfigure = (sp, options) =>
                options
                    .UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>())
                    .UseSnakeCaseNamingConvention();

            services.AddDbContext<AppDbContext>(dbConfigure);
            services.AddDbContextFactory<AppDbContext>(dbConfigure, ServiceLifetime.Scoped);

            Program.Container.Options.AllowOverridingRegistrations = true;
        });
    }

    /// <summary>
    /// Creates a DI scope for resolving scoped services (e.g. IDbContextFactory) outside
    /// of an HTTP request — used by tests to seed and assert against the database. The
    /// DbContext and its factory are Scoped, so they cannot be resolved directly from the
    /// root provider (<see cref="WebApplicationFactory{TEntryPoint}.Services"/>).
    /// </summary>
    public AsyncServiceScope CreateScope() => Services.CreateAsyncScope();

    public async Task ResetDatabase()
    {
        await using var connection = this
            .Services.GetRequiredService<NpgsqlDataSource>()
            .CreateConnection();
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }
}
