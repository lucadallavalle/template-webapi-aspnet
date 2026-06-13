# ASP.NET Core WebApi template

This template can be used to bootstrap a working full-fledged ASP.NET Web Api project with a single CLI command (see below).

It contains what I consider to be best practices/patterns, such as CQRS, Mediator, Clean Architecture.

## :star: Like it? Give a star
If you like this project, you learned something from it or you are using it in your applications, please press the star button. Thanks!

## Motivation
I found implementations of similar samples/templates to often be overly complicated and over-engineered (IMO). This is an effort to create a more approachable, more maintainable solution that can be used as a starting point for the majority of real-world projects while, at the same time, striving to reach a sensible balance between flexibility and complexity.

## Features
- Based on .NET 10 to have access to the latest features
- Minimal hosting model (top-level statements in `Program.cs`)
- [CQRS](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs): commands and queries have separate handlers and decorator pipelines (dispatched through a mediator), and the data access is segregated too — read repositories for queries, write repositories behind the unit of work for commands
- Simple [Mediator](https://en.wikipedia.org/wiki/Mediator_pattern) abstraction for CQRS and implementation relying on the chosen Dependency Injection container (see [HumbleMediator](https://github.com/undrivendev/HumbleMediator))
- Project structure following [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) principles
- Read/write repository split (the data-access half of CQRS) over [Entity Framework Core](https://github.com/dotnet/efcore), behind tech-agnostic interfaces — `Application`/`Core` never reference EF, so a repository can be backed by [Dapper](https://dapperlib.github.io/Dapper/) or raw ADO.NET without touching business logic:
  - **Reads** ([`IReadRepository<T>`](src/Core/Persistence/IReadRepository.cs)) are injected directly into query handlers and run on a fresh no-tracking context — no transaction, and a natural seam to later target a read replica
  - **Writes** ([`IWriteRepository<T, TKey>`](src/Core/Persistence/IWriteRepository.cs)) are resolved inside the unit of work: the delegate passed to `IUnitOfWorkFactory.ExecuteInTransactionAsync` **is** the atomic, retriable transaction — `uow.GetRepository<T>()` stages changes while the unit of work flushes and commits on success (and rolls back on exception)
- Automatic repository registration: read repositories (`ReadRepositoryBase<>`) and write repositories (`WriteRepositoryBase<,,>`) are discovered in the `Infrastructure` assembly and registered by a single [`AddPersistence<TDbContext>()`](src/Infrastructure/Persistence/ServiceCollectionExtensions.cs) call (which also wires the unit-of-work factory for the write side)
- Read repositories ship built-in pagination, sorting, and free-text search scaffolding (`PagedResult<T>`, `ListAsync`)
- [PostgreSQL](https://www.postgresql.org/) open source database as data store (easily replaceable with any Entity Framework-supported data stores)
- Connection resiliency: transient database failures are retried automatically ([`EnableRetryOnFailure`](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency)); on a retry the unit-of-work delegate is replayed on a fresh `DbContext`, which is why it must stay idempotent (no non-database side effects inside the transaction). Replay makes writes **at-least-once** (a lost commit acknowledgement can replay an already-committed transaction); for **exactly-once**, pass a `verifySucceeded` callback to `ExecuteInTransactionAsync` — write a unique token row inside the transaction and report `IsCommitted` by checking for that row, so the strategy can confirm the prior commit instead of repeating it. Run that verification query on a **retry-enabled** context (e.g. one from the same `IDbContextFactory`, which already has `EnableRetryOnFailure`): the connection that failed during commit is likely to fail again during verification. This is EF Core's [_Option 4 — Manually track the transaction_](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency#transaction-commit-failure-and-the-idempotency-issue)
- Database configured to use snake_case naming convention via [EFCore.NamingConventions](https://github.com/efcore/EFCore.NamingConventions)
- Migrations handled by Entity Framework and automatically applied during startup (in dev environment)
- [SimpleInjector](https://simpleinjector.org/) open-source DI container integration for advanced service registration scenarios
- [Aspect-oriented programming](https://en.wikipedia.org/wiki/Aspect-oriented_programming) using [Decorators](https://en.wikipedia.org/wiki/Decorator_pattern) on the above-mentioned mediator
  - Logging: [QueryHandlerLoggingDecorator](src/Application/Logging/QueryHandlerLoggingDecorator.cs) and [CommandHandlerLoggingDecorator](src/Application/Logging/CommandHandlerLoggingDecorator.cs)
  - Validation: [CommandHandlerValidationDecorator](src/Application/Validation/CommandHandlerValidationDecorator.cs) and [QueryHandlerValidationDecorator](src/Application/Validation/QueryHandlerValidationDecorator.cs)
- Caching is intentionally **not** a mediator decorator — it's a per-resource policy (key scope, TTL, invalidation), not a uniform cross-cutting concern like logging or validation. Reach for purpose-built layers instead: [output caching](https://learn.microsoft.com/aspnet/core/performance/caching/output) for HTTP responses, and [`HybridCache`](https://learn.microsoft.com/aspnet/core/performance/caching/hybrid) used deliberately per-handler (with explicit, scoped keys and invalidation) for application-level caching
- Structured logging using the standard [MEL](https://github.com/dotnet/runtime/tree/main/src/libraries/Microsoft.Extensions.Logging.Abstractions) interface with the open-source [Serilog](https://serilog.net/) logging library implementation
- Cache-friendly [Dockerfile](src/WebApi/Dockerfile) with a `/health` container HEALTHCHECK
- Expressive testing using [xUnit](https://xunit.net/) and [AwesomeAssertions](https://github.com/AwesomeAssertions/AwesomeAssertions)
- Integration testing using real database implementation with [Testcontainers](https://dotnet.testcontainers.org/)
- [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/Central-Package-Management)
- Continuous integration via [GitHub Actions](.github/workflows/ci.yml): build, dependency vulnerability scan with [grype](https://github.com/anchore/grype), and Docker image build
- [pre-commit](https://pre-commit.com/) hooks: C# formatting with [CSharpier](https://csharpier.com/), secret scanning with [gitleaks](https://github.com/gitleaks/gitleaks), and general file-hygiene checks

## Usage
### 1. Bootstrap your project
Here are a couple of ways to bootstrap a new project starting from this template.

#### dotnet new template (Recommended)
The easiest way to create a new project from this template:

```bash
dotnet new install .
dotnet new webapi-undrivendev -n YourProjectName -o ./YourProjectName
```

👉 **See the complete guide: [TEMPLATE-SETUP.md](TEMPLATE-SETUP.md)**

#### Cookiecutter template
Probably the best way to bootstrap this project, with just one command, but some dependencies are needed.
1. Make sure Python is installed
2. Install [cookiecutter](https://www.cookiecutter.io/).
3. Bootstrap initial project with the following command: `cookiecutter gh:undrivendev/template-webapi-aspnet --checkout cookiecutter`
#### GitHub template
You could use this project as a GitHub template and clone it in your personal account by using the `Use this template` green button on the top of the page.

Then you'd have to rename classes and namespaces.


### 2. Apply initial migration
When you have the project ready, it's time to create the initial migration using [dotnet-ef](https://docs.microsoft.com/en-us/ef/core/cli/dotnet) (or if you use Rider, like me, you can try [this plugin](https://plugins.jetbrains.com/plugin/18147-entity-framework-core-ui)).

Here's an example command using the default solution name, if you changed it you would have to adapt it accordingly:

```sh
dotnet ef migrations add --project ./src/Infrastructure/Infrastructure.csproj --context AppDbContext --startup-project ./src/WebApi/WebApi.csproj InitialMigration
```

The above migration is applied automatically during startup in the dev environment.

> **Enable tests in CI:** the integration tests create their schema by migrating on
> startup, so they need at least one migration. Once you've added the migration above,
> uncomment the `dotnet test` step in [.github/workflows/ci.yml](.github/workflows/ci.yml)
> to run the full suite on every pull request.

### 3. Configure authentication & authorization
This template wires `app.UseAuthentication()` and `app.UseAuthorization()` into the request pipeline, but **no authentication scheme is configured** — until you add one, every request is anonymous and (absent any `[Authorize]` attributes) allowed through. Before exposing the API, register a real scheme and add authorization policies / `[Authorize]` attributes, e.g.:

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* authority, audience, ... */ });
```

Like the initial migration above, this is a deliberate "you must configure it" step: the middleware ships wired so you only have to plug in your scheme and policies.

### 4. Start the application
The default API endpoints should be testable from the [Swagger UI](http://localhost:5000/swagger/index.html).

Enjoy!

### 5. CI/CD
This template ships a CI workflow at [.github/workflows/ci.yml](.github/workflows/ci.yml) that runs on every pull request: it restores and builds the solution, scans dependencies with [grype](https://github.com/anchore/grype), and builds the Docker image. The `dotnet test` step is commented out until you add your first migration (see step 2).

It does **not** ship a release/deployment pipeline — deploy targets vary too much to template usefully. You need to create your own: typically, on push to `main`, build and push the image from [src/WebApi/Dockerfile](src/WebApi/Dockerfile) to your container registry, then trigger a deploy to your host.
