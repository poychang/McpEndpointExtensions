# McpEndpointExtensions

[English](README.md)

McpEndpointExtensions 是一個小型 ASP.NET Core 擴充程式庫，可讓單一應用程式同時裝載多個 Model Context Protocol (MCP) Streamable HTTP endpoint，並讓每個 endpoint 暴露不同的工具集合。

當單一 MCP server 需要依路由隔離工具時，可以使用此程式庫，例如：

- `/domain1/mcp` 只會列出並呼叫 domain 1 工具。
- `/domain2/mcp` 只會列出並呼叫 domain 2 工具。
- 在某個 endpoint 註冊的工具，若透過另一個 endpoint 呼叫，會被拒絕。

## 專案狀態

此 repository 目前包含程式庫原始碼與可執行的範例應用程式。程式庫目標框架為 `net10.0`，並依賴 `ModelContextProtocol.AspNetCore` `1.3.0`。

## 運作方式

此程式庫加入兩個擴充方法：

- `WithMcpEndpoint(pattern, tools)` 註冊指定路由 pattern 下可用的 MCP 工具。
- `MapMcpEndpoint()` 使用 MCP ASP.NET Core transport 對每個已註冊的路由 pattern 進行對應。

執行時，程式庫會讀取目前的 `HttpContext.Request.Path`，並使用該 path 判斷 MCP `tools/list` 與 `tools/call` request 可用的工具。

## 安裝

從 NuGet 安裝：

```powershell
dotnet add package ModelContextProtocol.Extensions.AspNetCore.Endpoints
```

或從 ASP.NET Core MCP server 專案直接參考此程式庫專案：

```xml
<ProjectReference Include="..\..\src\McpEndpointExtensions.csproj" />
```

應用程式也會透過此程式庫相依性使用 MCP ASP.NET Core server package。

## 使用方式

註冊 MCP server、設定 HTTP transport，並針對每組依路由區分的工具集合呼叫一次 `WithMcpEndpoint`：

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

使用一般的 `ModelContextProtocol.Server` API 定義工具：

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

## 行為

- 每次呼叫 `WithMcpEndpoint` 都會將一個 route pattern 與一組指定的 `McpServerTool` instance 建立關聯。
- `MapMcpEndpoint()` 會對應所有已設定的 MCP endpoint pattern。若沒有註冊任何 endpoint，會擲出 `InvalidOperationException`。
- `tools/list` 只會傳回與目前 endpoint path 相關聯的工具。
- `tools/call` 只會呼叫與目前 endpoint path 相關聯的工具。
- 呼叫目前 endpoint 不可用的工具時，會傳回 MCP `InvalidParams` protocol error。
- `WithMcpEndpoint` 會註冊 `IHttpContextAccessor`；應用程式不需要為了此程式庫另外註冊。

## Repository 結構

- `src/` - `McpEndpointExtensions` 程式庫。
- `samples/McpEndpointServer/` - 可執行的 ASP.NET Core 範例，用來示範多個 MCP endpoint。
- `tests/` - 保留供測試使用。

## 建置

使用 .NET SDK 建置 solution：

```powershell
dotnet build ModelContextProtocol.slnx
```

此 solution 已使用 .NET SDK `10.0.300` 驗證。

## NuGet 發佈流程

此 repository 透過 GitHub Actions 發佈套件。

1. CI workflow (`.github/workflows/ci.yml`) 會在 push/PR 時執行 restore、build、test 與 pack 檢查。
2. 發佈 workflow (`.github/workflows/publish-nuget.yml`) 會在推送版本 tag（例如 `v1.2.3`）或手動觸發時執行。
3. 發佈作業使用 `dotnet nuget push` 發佈到 `https://api.nuget.org/v3/index.json`。

### 必要的 GitHub 設定

發佈前請先設定以下 secret：

- `NUGET_API_KEY`：具備套件推送權限的 NuGet API key。

建議設定流程：

1. 建立名為 `nuget-publish` 的 GitHub environment。
2. 將 `NUGET_API_KEY` 設定為 environment secret。
3. 為該 environment 設定審核者與/或分支、標籤保護規則。

## 授權

此專案採用 MIT 授權。詳細內容請參閱 [LICENSE](LICENSE)。
