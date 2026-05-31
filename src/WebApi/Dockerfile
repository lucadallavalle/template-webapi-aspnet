FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build

# copy csproj and props files, then restore as distinct layers for caching
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY src/Core/Core.csproj ./src/Core/
COPY src/Application/Application.csproj ./src/Application/
COPY src/Infrastructure/Infrastructure.csproj ./src/Infrastructure/
COPY src/WebApi/WebApi.csproj ./src/WebApi/
RUN dotnet restore -r linux-x64 "./src/WebApi/WebApi.csproj"

# copy source and publish
COPY src/Core/ ./src/Core/
COPY src/Application/ ./src/Application/
COPY src/Infrastructure/ ./src/Infrastructure/
COPY src/WebApi/ ./src/WebApi/
RUN dotnet publish -c Release --no-self-contained -r linux-x64 -o /app "./src/WebApi/WebApi.csproj"

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
WORKDIR /app
COPY --from=build /app .
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -sf http://localhost:5000/health || exit 1
USER app
ENTRYPOINT ["dotnet", "WebApiTemplate.WebApi.dll"]
