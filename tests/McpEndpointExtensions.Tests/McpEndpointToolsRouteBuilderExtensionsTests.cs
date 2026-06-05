using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace McpEndpointExtensions.Tests;

[TestClass]
public sealed class McpEndpointToolsRouteBuilderExtensionsTests
{
    [TestMethod]
    public void MapMcpEndpoint_ThrowsWhenNoEndpointsWereConfigured()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddMcpServer();
        using var app = builder.Build();

        var exception = ExceptionAssert.Throws<InvalidOperationException>(app.MapMcpEndpoint);

        StringAssert.Contains(exception.Message, "WithMcpEndpoint");
    }

    [TestMethod]
    public void MapMcpEndpoint_MapsEveryConfiguredEndpointPattern()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services
            .AddMcpServer()
            .WithHttpTransport(options => options.Stateless = true)
            .WithMcpEndpoint("/domain1/mcp", TestTools.Domain1A)
            .WithMcpEndpoint("/domain2/mcp", TestTools.Domain2B);
        using var app = builder.Build();

        app.MapMcpEndpoint();

        var routePatterns = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .Select(pattern => pattern?.TrimEnd('/'))
            .ToArray();

        Assert.IsTrue(routePatterns.Contains("/domain1/mcp"), string.Join(Environment.NewLine, routePatterns));
        Assert.IsTrue(routePatterns.Contains("/domain2/mcp"), string.Join(Environment.NewLine, routePatterns));
    }
}
