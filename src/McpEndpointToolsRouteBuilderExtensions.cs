using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to map the MCP endpoints that were configured
/// through <c>WithMcpEndpoint</c>.
/// </summary>
public static class McpEndpointToolsRouteBuilderExtensions
{
    /// <summary>
    /// Maps Streamable HTTP endpoints for every route pattern registered with <c>WithMcpEndpoint</c>.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to add the MCP endpoints to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="endpoints"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No endpoints have been configured with <c>WithMcpEndpoint</c>.</exception>
    public static void MapMcpEndpoint(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<McpEndpointToolOptions>>().Value;

        if (options.Patterns.Count == 0)
        {
            throw new InvalidOperationException(
                "No MCP endpoints have been configured. Call WithMcpEndpoint(...) on the IMcpServerBuilder before calling MapMcpEndpoint().");
        }

        foreach (var pattern in options.Patterns)
        {
            endpoints.MapMcp(pattern);
        }
    }
}
