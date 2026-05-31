using System.Globalization;
using FluentValidation;
using HumbleMediator;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using Serilog.Events;
using SimpleInjector;
using WebApiTemplate.Application;
using WebApiTemplate.Application.Customers.Commands;
using WebApiTemplate.Application.Customers.Queries;
using WebApiTemplate.Application.Logging;
using WebApiTemplate.Application.Validation;
using WebApiTemplate.Core;
using WebApiTemplate.Core.Customers;
using WebApiTemplate.Infrastructure.Customers;
using WebApiTemplate.Infrastructure.Persistence;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

try
{
    Log.Information("Starting web host");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog(); // replace built-in logging with Serilog

    // Add services to the container.
    builder.Services.AddControllers();

    // swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        // Infer required/nullable from C# nullability annotations (NRT): `string Name`
        // becomes required + non-nullable, `string? Foo` stays optional + nullable. Gives
        // the generated TypeScript client accurate types.
        options.SupportNonNullableReferenceTypes();
        options.NonNullableReferenceTypesAsRequired();

        // Derive operationId from controller + action for clean client generation,
        // e.g. CustomersController.GetById -> "Customers_GetById".
        options.CustomOperationIds(apiDesc =>
            apiDesc.ActionDescriptor is ControllerActionDescriptor action
                ? $"{action.ControllerName}_{action.ActionName}"
                : null
        );

        // Surface the XML doc comments (/// ...) in the OpenAPI document. Every project
        // sets GenerateDocumentationFile=true, so include each app assembly's XML from the
        // output directory.
        foreach (var xml in Directory.GetFiles(AppContext.BaseDirectory, "WebApiTemplate.*.xml"))
        {
            options.IncludeXmlComments(xml, includeControllerXmlComments: true);
        }
    });

    builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

    // persistence
    var connString =
        builder.Configuration.GetConnectionString("Default")
#pragma warning disable CA2208
        ?? throw new ArgumentNullException("connectionString");
#pragma warning restore CA2208
    builder.Services.AddNpgsqlDataSource(connString);

    Action<IServiceProvider, DbContextOptionsBuilder> dbConfigure = (sp, options) =>
        options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>()).UseSnakeCaseNamingConvention();

    // AppDbContext is registered Scoped (the EF Core default) — never Singleton. A
    // DbContext is not thread-safe, so a singleton would be shared across concurrent
    // requests (e.g. the /health AddDbContextCheck below). The unit-of-work write path
    // does not use this registration directly: it creates a fresh context per unit of
    // work from the factory below.
    //
    // The factory is Scoped (not pooled). A pooled factory is itself a singleton, so it
    // would have to resolve EF's now-Scoped IDbContextOptionsConfiguration<AppDbContext>
    // from the root provider, which throws. Keeping the context and its factory both
    // Scoped aligns the option-configuration lifetimes.
    builder.Services.AddDbContext<AppDbContext>(dbConfigure);
    builder.Services.AddDbContextFactory<AppDbContext>(dbConfigure, ServiceLifetime.Scoped);
    builder.Services.AddDistributedMemoryCache();

    // Required by SimpleInjector's ASP.NET Core integration to resolve cross-wired
    // *scoped* services (e.g. IDbContextFactory<AppDbContext>) from the active request's
    // IServiceProvider. Without it those resolve from the root provider and trip MS-DI
    // scope validation.
    builder.Services.AddHttpContextAccessor();

    // SimpleInjector
    var container = Container;
    // Scoped is the correct default lifestyle for a per-request web app: handlers,
    // repositories and the unit-of-work factory resolve once per request instead of as
    // process-wide singletons that would capture state across requests. The AspNetCore
    // integration opens an async scope per request (and a verification scope for
    // container.Verify()).
    container.Options.DefaultLifestyle = Lifestyle.Scoped;
    builder.Services.AddSimpleInjector(
        container,
        options =>
        {
            options.AddAspNetCore().AddControllerActivation();

            // IDbContextFactory<AppDbContext> is Scoped — cross-wire it explicitly so
            // SimpleInjector resolves it from the active request scope. Auto cross-wiring
            // resolves it from the root provider, which trips MS-DI's scope validation.
            options.CrossWire<IDbContextFactory<AppDbContext>>();
        }
    );

    container.Register<ICustomerReadRepository, CustomerReadRepository>();
    container.Register<ICustomerWriteRepository, CustomerWriteRepository>();
    container.Register<IUnitOfWorkFactory, UnitOfWorkFactory>();

    // validators
    container.Collection.Register(
        typeof(IValidator<>),
        typeof(GetCustomerByIdQueryValidator).Assembly
    );

    // mediator
    container.Register<IMediator>(() => new Mediator(container.GetInstance));

    // mediator handlers
    container.Register(typeof(ICommandHandler<,>), typeof(CreateCustomerCommandHandler).Assembly);
    container.Register(typeof(IQueryHandler<,>), typeof(GetCustomerByIdQueryHandler).Assembly);

    // mediator handlers decorators - queries pipeline
    container.RegisterDecorator(
        typeof(IQueryHandler<,>),
        typeof(QueryHandlerValidationDecorator<,>)
    );
    container.RegisterDecorator(typeof(IQueryHandler<,>), typeof(QueryHandlerCachingDecorator<,>));
    container.RegisterDecorator(typeof(IQueryHandler<,>), typeof(QueryHandlerLoggingDecorator<,>));

    // mediator handlers decorators - commands pipeline
    container.RegisterDecorator(
        typeof(ICommandHandler<,>),
        typeof(CommandHandlerValidationDecorator<,>)
    );
    container.RegisterDecorator(
        typeof(ICommandHandler<,>),
        typeof(CommandHandlerLoggingDecorator<,>)
    );

    var app = builder.Build();

    app.Services.UseSimpleInjector(container);

    // Apply pending EF Core migrations automatically in development mode.
    // To do that in production, especially in multi-instance scenarios, you need
    // to make sure that migrations are applied as a separate deploy step to prevent data corruption.
    if (app.Environment.IsDevelopment())
    {
        // AppDbContext is Scoped, so it must be resolved from a scope rather than the
        // root provider.
        await using var migrationScope = app.Services.CreateAsyncScope();
        var dbContext = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (dbContext.Database.IsRelational())
        {
            // It will throw if the db is not relational
            await dbContext.Database.MigrateAsync();
        }
    }

    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.MapHealthChecks("/health");

    app.UseAuthorization();
    app.MapControllers();

    container.Verify();

    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// The Program class. This is added support accessing the <see cref="Container"/> instance.
/// </summary>
public partial class Program
{
    /// <summary>
    /// The <see cref="Container"/> instance.
    /// </summary>
    public static readonly Container Container = new();
}
