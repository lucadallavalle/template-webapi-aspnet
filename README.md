# ASP.NET Core WebApi template

This template can be used to bootstrap a working full-fledged ASP.NET Web Api project with a single CLI command (see below).

It contains what I consider to be best practices/patterns, such as CQRS, Mediator, Clean Architecture.

## :star: Like it? Give a star
If you like this project, you learned something from it or you are using it in your applications, please press the star button. Thanks!

## Motivation
I found implementations of similar samples/templates to often be overly complicated and over-engineered (IMO). This is an effort to create a more approachable, more maintainable solution that can be used as a starting point for the majority of real-world projects while, at the same time, striving to reach a sensible balance between flexibility and complexity.

## Features
- Based on .NET 10 to have access to the latest features
- Simplified Startup.cs hosting model
- [CQRS](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs) with full separation between Read and Write repositories
- Simple [Mediator](https://en.wikipedia.org/wiki/Mediator_pattern) abstraction for CQRS and implementation relying on the chosen Dependency Injection container (see [HumbleMediator](https://github.com/undrivendev/HumbleMediator))
- Project structure following [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) principles
- Read and write repositories based on [Entity Framework Core](https://github.com/dotnet/efcore), behind tech-agnostic `IReadRepository`/`IWriteRepository` interfaces — `Application`/`Core` never reference EF, so a repository can be backed by [Dapper](https://dapperlib.github.io/Dapper/) or raw ADO.NET without touching business logic
- Read repositories ship built-in pagination, sorting, and free-text search scaffolding (`PagedResult<T>`, `ListAsync`)
- [PostgreSQL](https://www.postgresql.org/) open source database as data store (easily replaceable with any Entity Framework-supported data stores)
- Database configured to use snake_case naming convention via [EFCore.NamingConventions](https://github.com/efcore/EFCore.NamingConventions)
- Migrations handled by Entity Framework and automatically applied during startup (in dev environment)
- [SimpleInjector](https://simpleinjector.org/) open-source DI container integration for advanced service registration scenarios
- [Aspect-oriented programming](https://en.wikipedia.org/wiki/Aspect-oriented_programming) using [Decorators](https://en.wikipedia.org/wiki/Decorator_pattern) on the above-mentioned mediator
  - Logging: [QueryHandlerLoggingDecorator](src/Application/Logging/QueryHandlerLoggingDecorator.cs) and [CommandHandlerLoggingDecorator](src/Application/Logging/CommandHandlerLoggingDecorator.cs)
  - Caching: [QueryHandlerCachingDecorator](src/Application/QueryHandlerCachingDecorator.cs)
  - Validation: [CommandHandlerValidationDecorator](src/Application/Validation/CommandHandlerValidationDecorator.cs) and [QueryHandlerValidationDecorator](src/Application/Validation/QueryHandlerValidationDecorator.cs)
- Structured logging using the standard [MEL](https://github.com/dotnet/runtime/tree/main/src/libraries/Microsoft.Extensions.Logging.Abstractions) interface with the open-source [Serilog](https://serilog.net/) logging library implementation
- Cache-friendly [Dockerfile](src/WebApi/Dockerfile)
- Expressive testing using [xUnit](https://xunit.net/) and [AwesomeAssertions](https://github.com/AwesomeAssertions/AwesomeAssertions)
- Integration testing using real database implementation with [Testcontainers](https://dotnet.testcontainers.org/)
- [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/Central-Package-Management)

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

### 3. Start the application
The default API endpoints should be testable from the [Swagger UI](http://localhost:5000/swagger/index.html).

Enjoy!

### 4. CI/CD
This template ships a CI workflow at [.github/workflows/ci.yml](.github/workflows/ci.yml) that runs on every pull request: it restores and builds the solution, scans dependencies with [grype](https://github.com/anchore/grype), and builds the Docker image. The `dotnet test` step is commented out until you add your first migration (see step 2).

It does **not** ship a release/deployment pipeline — deploy targets vary too much to template usefully. You need to create your own: typically, on push to `main`, build and push the image from [src/WebApi/Dockerfile](src/WebApi/Dockerfile) to your container registry, then trigger a deploy to your host.
