using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ModelContextProtocol.AspNetCore;

/// <summary>
/// Configures the Model Context Protocol server tool handlers so that each HTTP endpoint only lists and
/// invokes the tools that were associated with it through <see cref="McpEndpointToolOptions"/>.
/// </summary>
internal sealed class McpEndpointToolHandlerSetup : IConfigureOptions<McpServerOptions>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly McpEndpointToolOptions _endpointToolOptions;

    public McpEndpointToolHandlerSetup(IHttpContextAccessor httpContextAccessor, IOptions<McpEndpointToolOptions> endpointToolOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _endpointToolOptions = endpointToolOptions.Value;
    }

    public void Configure(McpServerOptions options)
    {
        options.Capabilities ??= new();
        options.Capabilities.Tools ??= new();
        options.Handlers.ListToolsHandler = HandleListToolsAsync;
        options.Handlers.CallToolHandler = HandleCallToolAsync;
    }

    private ValueTask<ListToolsResult> HandleListToolsAsync(RequestContext<ListToolsRequestParams> context, CancellationToken cancellationToken)
    {
        var tools = GetToolsForCurrentRequest();

        return ValueTask.FromResult(new ListToolsResult
        {
            Tools = [.. tools.Select(tool => tool.ProtocolTool)]
        });
    }

    private async ValueTask<CallToolResult> HandleCallToolAsync(RequestContext<CallToolRequestParams> context, CancellationToken cancellationToken)
    {
        var requestedToolName = context.Params.Name;
        var tool = GetToolsForCurrentRequest().FirstOrDefault(candidate => candidate.ProtocolTool.Name == requestedToolName);

        if (tool is null)
        {
            throw new McpProtocolException(
                $"Tool '{requestedToolName}' is not available on this endpoint.",
                McpErrorCode.InvalidParams);
        }

        return await tool.InvokeAsync(context, cancellationToken);
    }

    private IReadOnlyList<McpServerTool> GetToolsForCurrentRequest()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        return httpContext is null
            ? []
            : _endpointToolOptions.GetToolsForPath(httpContext.Request.Path);
    }
}
