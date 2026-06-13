# Using This Project as a .NET Template

This guide explains how to install and use this project as a `dotnet new` template.

## Installation

### Option 1: Install from Local Directory
From this project's root directory, run:

```bash
dotnet new install .
```

### Option 2: Install from NuGet (if you package it)
```bash
dotnet new install YourPackageName
```

### Option 3: Install from Git Repository
```bash
dotnet new install <repository-url>
```

## Usage

Once installed, create a new project using:

```bash
# Create in current directory
dotnet new webapi-undrivendev -n MyAwesomeApi

# Create in a new directory
dotnet new webapi-undrivendev -n MyAwesomeApi -o ./MyAwesomeApi
```

### Template Parameters

- `-n|--name`: The name for the output being created (default: MyWebApi)
- `-o|--output`: Location to place the generated output
- `--skipRestore`: Skip automatic NuGet package restore

### Example

```bash
dotnet new webapi-undrivendev -n ProductCatalogApi -o ./ProductCatalogApi
cd ProductCatalogApi
```

This will:
1. Create a new project with all files
2. Replace "WebApiTemplate" with "ProductCatalogApi" throughout the codebase
3. Rename files and folders accordingly
4. Automatically run `dotnet restore`

## What Gets Replaced

The template engine will automatically replace:
- Solution name: `WebApiTemplate.sln` → `YourProjectName.sln`
- Namespaces: `WebApiTemplate.*` → `YourProjectName.*`
- Project references
- Assembly names

## Uninstalling the Template

To remove the template:

```bash
dotnet new uninstall WebApiTemplate.CleanArchitecture
```

Or if installed from a path:

```bash
dotnet new uninstall /path/to/template-webapi-aspnet
```

## List Installed Templates

To see all installed templates:

```bash
dotnet new list
```

## Publishing Your Template (Optional)

### 1. Create a NuGet Package

Create a `.nuspec` file:

```xml
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd">
  <metadata>
    <id>YourCompany.WebApiTemplate</id>
    <version>1.0.0</version>
    <description>ASP.NET Core WebAPI template with Clean Architecture</description>
    <authors>Your Name</authors>
    <packageTypes>
      <packageType name="Template" />
    </packageTypes>
  </metadata>
  <files>
    <file src="**" exclude="**\bin\**;**\obj\**;**\.git\**" />
  </files>
</package>
```

### 2. Pack and Publish

```bash
nuget pack
dotnet nuget push YourCompany.WebApiTemplate.1.0.0.nupkg -s https://api.nuget.org/v3/index.json -k YOUR_API_KEY
```

## Testing the Template Locally

Before publishing, test the template:

```bash
# Install
dotnet new install .

# Create a test project
dotnet new webapi-undrivendev -n TestProject -o ../test-output

# Verify the output
cd ../test-output
dotnet build
dotnet test

# Clean up
cd ..
rm -rf test-output

# Uninstall
dotnet new uninstall .
```

## Customizing the Template

Edit `.template.config/template.json` to:
- Add more parameters (database type, authentication method, etc.)
- Change the short name or classifications
- Add conditional file inclusion
- Customize post-actions

Example of adding a parameter:

```json
"symbols": {
  "UseDocker": {
    "type": "parameter",
    "datatype": "bool",
    "defaultValue": "true",
    "description": "Include Docker support"
  }
}
```

## Next Steps After Creating a New Project

1. **Update connection strings** in `appsettings.json`
2. **Run initial migration**:
   ```bash
   dotnet ef migrations add InitialMigration --project ./src/Infrastructure --startup-project ./src/WebApi
   ```
3. **Configure authentication & authorization**: `UseAuthentication()`/`UseAuthorization()` are wired into the pipeline but no scheme is registered (all requests are anonymous until you add one). Register a real scheme (e.g. JWT bearer) and add authorization policies / `[Authorize]` attributes before exposing the API.
4. **Start the application**:
   ```bash
   dotnet run --project ./src/WebApi
   ```
5. **Access Swagger UI**: http://localhost:5000/swagger/index.html

## Troubleshooting

### Template not showing after install
```bash
dotnet new list | grep webapi-undrivendev
```

### Permission errors on Linux/macOS
```bash
chmod -R 755 .
```

### Clear template cache
```bash
dotnet new --debug:reinit
```
