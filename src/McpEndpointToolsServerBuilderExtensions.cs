using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IMcpServerBuilder"/> to expose a different set of tools per HTTP endpoint.
/// </summary>
public static class McpEndpointToolsServerBuilderExtensions
{
    /// <summary>
    /// Associates a set of tools with a Model Context Protocol HTTP endpoint so that only those tools are listed
    /// and callable when a client connects to the matching route.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="pattern">The route pattern prefix the tools are exposed under, for example <c>/domain1/mcp</c>.</param>
    /// <param name="tools">The tools to expose on the endpoint.</param>
    /// <returns>The builder provided in <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="tools"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is <see langword="null"/> or empty.</exception>
    /// <remarks>
    /// <para>
    /// Call this method once per endpoint. Every endpoint registered here must also be mapped during application
    /// startup by calling <c>MapMcpEndpoint</c> on the endpoint route builder.
    /// </para>
    /// <para>
    /// This method registers <c>IHttpContextAccessor</c> so the active request path can be resolved while listing
    /// and calling tools; there is no need to call <c>AddHttpContextAccessor</c> separately.
    /// </para>
    /// </remarks>
    public static IMcpServerBuilder WithMcpEndpoint(this IMcpServerBuilder builder, string pattern, params McpServerTool[] tools)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(pattern);
        ArgumentNullException.ThrowIfNull(tools);

        builder.Services.AddHttpContextAccessor();
        builder.Services.Configure<McpEndpointToolOptions>(options => options.MapToolsToEndpoint(pattern, tools));
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<McpServerOptions>, McpEndpointToolHandlerSetup>());

        return builder;
    }
}
