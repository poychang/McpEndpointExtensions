# McpEndpointExtensions

[繁體中文](https://github.com/poychang/McpEndpointExtensions/blob/main/README.zh-tw.md)

McpEndpointExtensions is a small ASP.NET Core extension library for hosting multiple Model Context Protocol (MCP) Streamable HTTP endpoints from one application while exposing a different tool set from each endpoint.

Use it when a single MCP server needs route-level tool isolation, for example:

- `/domain1/mcp` lists and invokes only domain 1 tools.
- `/domain2/mcp` lists and invokes only domain 2 tools.
- A tool registered on one endpoint is rejected when it is called through another endpoint.

## Project Status

This repository contains the library source, a runnable ASP.NET Core sample application, and MSTest coverage for the endpoint registration and per-route tool filtering behavior. The library targets `net8.0`, `net9.0`, and `net10.0`. Its package version and `ModelContextProtocol.AspNetCore` dependency share the `Version` property in `src/McpEndpointExtensions.csproj`.

## How It Works

The library adds two extension methods:

- `WithMcpEndpoint(pattern, tools)` registers the MCP tools that should be available under a route pattern.
- `MapMcpEndpoint()` maps every registered route pattern with the MCP ASP.NET Core transport.

At runtime, the library reads the current `HttpContext.Request.Path` and uses that path to decide which tools are available for MCP `tools/list` and `tools/call` requests.

## Installation

Install from NuGet:

```powershell
dotnet add package PC.ModelContextProtocol.Extensions.AspNetCore.Endpoints
```

Or reference the library project directly from an ASP.NET Core MCP server project:

```xml
<ProjectReference Include="..\..\src\McpEndpointExtensions.csproj" />
```

The application must also use the MCP ASP.NET Core server package through this library dependency.

## Usage

Register the MCP server, configure the HTTP transport, and call `WithMcpEndpoint` once for each route-specific tool set:

```csharp
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithMcpEndpoint(
        "/domain1/mcp",
        McpServerTool.Create(Domain1Tools.A1),
        McpServerTool.Create(Domain1Tools.A2))
    .WithMcpEndpoint(
        "/domain2/mcp",
        McpServerTool.Create(Domain2Tools.B1));

var app = builder.Build();

app.MapMcpEndpoint();

app.Run();
```

Define tools with the normal `ModelContextProtocol.Server` APIs:

```csharp
using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public sealed class Domain1Tools
{
    [McpServerTool, Description("Echoes A1 calling.")]
    public static string A1(string input) => $"A1: {input}";

    [McpServerTool, Description("Echoes A2 calling.")]
    public static string A2(string input) => $"A2: {input}";
}
```

## Behavior

- Each call to `WithMcpEndpoint` associates one route pattern with a specific set of `McpServerTool` instances.
- `MapMcpEndpoint()` maps all configured MCP endpoint patterns. It throws an `InvalidOperationException` if no endpoints were registered.
- `tools/list` returns only the tools associated with the current endpoint path.
- `tools/call` invokes only tools associated with the current endpoint path.
- Calling a tool that is not available on the current endpoint returns an MCP `InvalidParams` protocol error.
- `WithMcpEndpoint` registers `IHttpContextAccessor`; the application does not need to register it separately for this library.

## Repository Layout

- `src/` - the `McpEndpointExtensions` library.
- `samples/McpEndpointServer/` - a runnable ASP.NET Core sample that demonstrates multiple MCP endpoints.
- `tests/` - MSTest test coverage for endpoint options, route mapping, service registration, and MCP tool handler behavior.

## Build

Use the .NET SDK to build the solution:

```powershell
dotnet build ModelContextProtocol.slnx
```

The solution was validated with .NET SDK `10.0.300`.

## NuGet publish workflow

This repository publishes the package through GitHub Actions.

1. CI workflow (`.github/workflows/ci.yml`) runs on push/PR and performs restore, build, test, and pack checks.
2. Publish workflow (`.github/workflows/publish-nuget.yml`) checks NuGet every Monday at 01:00 UTC (09:00 Taiwan time) for the latest stable `ModelContextProtocol.AspNetCore` release. GitHub may delay scheduled runs. Prereleases are excluded.
3. If a newer version is available, it updates the shared project/dependency version, restores, builds, and tests the solution, and packs the library, then commits the validated version to the default branch. For an unpublished version it creates a matching `v*` tag and dispatches a separate release run on that tag, which builds, tests, and publishes to `https://api.nuget.org/v3/index.json`. Releases are serialized and never downgrade the dependency.
4. If that package version is already published, it skips publishing. If a previous publish failed after committing the update, a later run retries the missing release from its existing tag when that tag is an ancestor of the validated commit and has the expected dependency version. A conflicting tag stops the workflow for manual resolution. Build/test failures prevent publication; incompatible upstream API changes require a manual fix.
5. Manual dispatch on the default branch with an empty version checks for updates immediately. A version tag (for example `v2.2.0`) or an explicit manual version must match the dependency version committed in the selected ref; arbitrary release versions are rejected.

### Required GitHub configuration

Publishing uses NuGet trusted publishing (OIDC), not a long-lived API key. Configure:

- `NUGET_USER`: NuGet username stored as a GitHub environment secret.

Recommended setup path:

1. Create a GitHub environment named `nuget-publish`.
2. Add `NUGET_USER` as an environment secret. On nuget.org, configure a trusted publishing policy for this repository, workflow file `publish-nuget.yml`, and environment `nuget-publish`.
3. Allow the `nuget-publish` environment to deploy from `v*` release tags. Required reviewers pause automatic publishing until approval; omit reviewers if fully unattended publishing is desired.
4. Allow GitHub Actions to write repository contents and dispatch workflows. Default-branch protection/rulesets must permit this workflow's validated version commits and release tags; otherwise the push fails and nothing is published. Scheduled workflows only run after this configuration is merged into the default branch.

Manual versions accept an optional leading `v`. If the existing version tag predates changes to the publishing workflow or version script, a new `v<version>-publish.<run-id>` tag runs the updated publishing configuration without moving the original tag. The NuGet package version remains unchanged.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
