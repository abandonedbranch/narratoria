## spec: dotnet-blazor-workspace-scaffold

mode:
  - isolated
  # pure function, no shared state
  # describes directory structure and minimal project scaffolding

behavior:
  - what: scaffold a blank .NET 10 Blazor Server workspace with required directories and project files
  - input:
      - string: target directory path (absolute)
      - string: project name (valid .NET identifier)
  - output:
      - void: workspace created at target path with directories and minimal project files
  - caller_obligations:
      - MUST provide writable target directory path
      - MUST provide valid .NET project name (alphanumeric, no spaces)
      - MUST have .NET 10 SDK installed
  - side_effects_allowed:
      - creates directory structure: src/, tests/, build/, scripts/
      - creates .NET project files (.csproj, .sln) via dotnet CLI
      - creates .gitignore with standard .NET entries
      - creates minimal Program.cs for Blazor Server

state:
  - none: stateless operation

preconditions:
  - target directory path exists and is writable
  - project name is valid .NET identifier
  - .NET 10 SDK is available in PATH

postconditions:
  - directory structure exists with src/, tests/, build/, scripts/
  - src/ contains {ProjectName}.csproj (Blazor Server, net10.0)
  - tests/ contains {ProjectName}.Tests.csproj (MSTest, net10.0)
  - solution file references both projects
  - Program.cs contains minimal Blazor Server bootstrap (no demo routes/components)
  - .gitignore excludes bin/, obj/, build outputs

invariants:
  - no demo code, placeholder components, or example routes
  - no third-party NuGet dependencies (only Microsoft.* first-party packages)
  - deterministic output (same input produces identical structure)
  - project files use only .NET 10 target framework

failure_modes:
  - IOException :: target path not writable :: log error_class=io_error, abort
  - InvalidProjectNameException :: project name invalid :: log error_class=validation_error, abort
  - DotNetSdkNotFoundException :: .NET 10 SDK not found :: log error_class=dependency_error, abort

policies:
  - no retry (file system operations are atomic)
  - no timeout (operation completes quickly)
  - idempotent: running multiple times on same target overwrites safely
  - no concurrency (single-threaded operation)
  - cancellation: not applicable (operation is synchronous and fast)

never:
  - NEVER include demo routes (/demo, /counter, /weather, /test-*)
  - NEVER include placeholder components (Counter, Weather, FetchData)
  - NEVER add third-party NuGet packages (non-Microsoft.*)
  - NEVER create sample data classes, mock services, or stub interfaces
  - NEVER add TODO comments or placeholder methods
  - NEVER include interactive tutorials or onboarding content

non_goals:
  - database setup
  - authentication/authorization scaffolding
  - CI/CD pipeline configuration
  - Docker/container support
  - cloud deployment configuration
  - development environment configuration (VS Code, Rider)
  - package manager lock files
  - dependency version pinning beyond framework version

performance:
  - completes in < 5 seconds for typical workspace
  - directory creation is O(1) per directory
  - file writes are sequential

observability:
  - logs:
      - operation_start: target_path, project_name
      - directory_created: path
      - project_created: project_path, framework
      - solution_created: solution_path
      - operation_complete: elapsed_ms, status
      - error: error_class, target_path, project_name, message
  - metrics:
      - workspace_scaffold_duration_ms
      - workspace_scaffold_success_count
      - workspace_scaffold_failure_count

output:
  - minimal implementation only (no commentary, no TODOs)

## Directory Structure

```
{target}/
├── src/
│   ├── {ProjectName}.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Components/
│   │   ├── App.razor
│   │   ├── Routes.razor
│   │   └── Layout/
│   │       ├── MainLayout.razor
│   │       └── MainLayout.razor.css
│   └── wwwroot/
│       ├── app.css
│       └── favicon.png
├── tests/
│   └── {ProjectName}.Tests/
│       ├── {ProjectName}.Tests.csproj
│       └── GlobalUsings.cs
├── build/
│   └── (empty, reserved for build artifacts)
├── scripts/
│   └── (empty, reserved for build/dev scripts)
├── .gitignore
└── {ProjectName}.sln
```

## Project Files Content

### src/{ProjectName}.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

### src/Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

### src/Components/App.razor

```razor
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link rel="stylesheet" href="app.css" />
    <HeadOutlet />
</head>
<body>
    <Routes />
    <script src="_framework/blazor.web.js"></script>
</body>
</html>
```

### src/Components/Routes.razor

```razor
<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

### src/Components/Layout/MainLayout.razor

```razor
@inherits LayoutComponentBase

<div class="page">
    <main>
        @Body
    </main>
</div>
```

### src/Components/Layout/MainLayout.razor.css

```css
.page {
    display: flex;
    flex-direction: column;
    min-height: 100vh;
}

main {
    flex: 1;
}
```

### src/wwwroot/app.css

```css
* {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
}

body {
    font-family: system-ui, -apple-system, sans-serif;
}
```

### src/appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### src/appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### src/Properties/launchSettings.json

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### tests/{ProjectName}.Tests/{ProjectName}.Tests.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="MSTest.TestAdapter" Version="3.7.0" />
    <PackageReference Include="MSTest.TestFramework" Version="3.7.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\{ProjectName}.csproj" />
  </ItemGroup>
</Project>
```

### tests/{ProjectName}.Tests/GlobalUsings.cs

```csharp
global using Microsoft.VisualStudio.TestTools.UnitTesting;
```

### .gitignore

```
# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Ww][Ii][Nn]32/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/
[Ll]ogs/

# Visual Studio cache/options
.vs/
.vscode/

# Build artifacts
build/

# User-specific files
*.rsuser
*.suo
*.user
*.userosscache
*.sln.docstates

# Test Results
[Tt]est[Rr]esult*/
[Bb]uild[Ll]og.*

# .NET
project.lock.json
project.fragment.lock.json
artifacts/

# Publishing
*.Publish.xml
*.pubxml
*.publishproj

# NuGet
*.nupkg
*.snupkg
**/packages/*
!**/packages/build/

# Others
*.cache
*.log
*.vspscc
*.vssscc
.builds
*.pidb
*.svclog
*.scc
```

## CLI Commands (Implementation Reference)

```bash
# Create directories
mkdir -p src tests build scripts

# Create solution
dotnet new sln -n {ProjectName}

# Create Blazor Server project
dotnet new blazor -n {ProjectName} -o src --no-https false

# Create test project
dotnet new mstest -n {ProjectName}.Tests -o tests/{ProjectName}.Tests

# Add projects to solution
dotnet sln add src/{ProjectName}.csproj
dotnet sln add tests/{ProjectName}.Tests/{ProjectName}.Tests.csproj

# Add project reference
dotnet add tests/{ProjectName}.Tests/{ProjectName}.Tests.csproj reference src/{ProjectName}.csproj

# Remove demo content (files to delete)
# src/Components/Pages/Counter.razor
# src/Components/Pages/Weather.razor
# src/Components/Pages/Error.razor
# Any other demo/example files

# Create required files with content as specified above
```
