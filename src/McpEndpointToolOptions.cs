using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace ModelContextProtocol.AspNetCore;

/// <summary>
/// Provides configuration for associating Model Context Protocol (MCP) tools with individual HTTP endpoints,
/// allowing a single MCP server to expose a different set of tools per route.
/// </summary>
/// <remarks>
/// Populate this options instance by calling <c>WithMcpEndpoint</c> on the <c>IMcpServerBuilder</c> during
/// application startup, or configure it directly via <c>services.Configure&lt;McpEndpointToolOptions&gt;(...)</c>.
/// </remarks>
public sealed class McpEndpointToolOptions
{
    private readonly Dictionary<string, IReadOnlyList<McpServerTool>> _toolsByPattern = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the route patterns that have been associated with a set of tools.
    /// </summary>
    public IReadOnlyCollection<string> Patterns => _toolsByPattern.Keys;

    /// <summary>
    /// Associates a set of tools with the endpoint identified by <paramref name="pattern"/>.
    /// </summary>
    /// <param name="pattern">The route pattern prefix that the tools are exposed under, for example <c>/domain1/mcp</c>.</param>
    /// <param name="tools">The tools to expose on the endpoint. Any tools previously associated with the same pattern are replaced.</param>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="tools"/> is <see langword="null"/>.</exception>
    public void MapToolsToEndpoint(string pattern, IEnumerable<McpServerTool> tools)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);
        ArgumentNullException.ThrowIfNull(tools);

        _toolsByPattern[pattern] = [.. tools];
    }

    /// <summary>
    /// Gets the tools registered for the endpoint that matches <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The request path to resolve tools for.</param>
    /// <returns>The tools registered for the matching endpoint, or an empty list when no endpoint matches.</returns>
    internal IReadOnlyList<McpServerTool> GetToolsForPath(PathString path)
    {
        foreach (var (pattern, tools) in _toolsByPattern)
        {
            if (path.StartsWithSegments(pattern))
            {
                return tools;
            }
        }

        return [];
    }
}
