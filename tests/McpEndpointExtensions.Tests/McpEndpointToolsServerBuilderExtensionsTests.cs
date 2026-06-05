using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

namespace McpEndpointExtensions.Tests;

[TestClass]
public sealed class McpEndpointToolsServerBuilderExtensionsTests
{
    [TestMethod]
    public void WithMcpEndpoint_RegistersEndpointOptionsAndHttpContextAccessor()
    {
        var services = new ServiceCollection();
        var builder = services.AddMcpServer();

        var returnedBuilder = builder
            .WithMcpEndpoint("/domain1/mcp", TestTools.Domain1A, TestTools.Domain1B)
            .WithMcpEndpoint("/domain2/mcp", TestTools.Domain2B);

        using var provider = services.BuildServiceProvider();
        var endpointOptions = provider.GetRequiredService<IOptions<McpEndpointToolOptions>>().Value;

        Assert.AreSame(builder, returnedBuilder);
        CollectionAssert.AreEquivalent(
            new[] { "/domain1/mcp", "/domain2/mcp" },
            endpointOptions.Patterns.ToArray());
        Assert.IsNotNull(provider.GetRequiredService<IHttpContextAccessor>());
    }

    [TestMethod]
    public void WithMcpEndpoint_RegistersSingleMcpOptionsSetupForMultipleEndpoints()
    {
        var services = new ServiceCollection();

        services
            .AddMcpServer()
            .WithMcpEndpoint("/domain1/mcp", TestTools.Domain1A)
            .WithMcpEndpoint("/domain2/mcp", TestTools.Domain2B);

        var setupRegistrations = services
            .Where(descriptor =>
                descriptor.ServiceType == typeof(IConfigureOptions<McpServerOptions>) &&
                descriptor.ImplementationType == typeof(McpEndpointToolHandlerSetup))
            .ToArray();

        Assert.HasCount(1, setupRegistrations);
    }

    [TestMethod]
    public void WithMcpEndpoint_ConfiguresMcpToolHandlers()
    {
        var services = new ServiceCollection();

        services
            .AddMcpServer()
            .WithMcpEndpoint("/domain1/mcp", TestTools.Domain1A);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<McpServerOptions>>().Value;

        Assert.IsNotNull(options.Capabilities);
        Assert.IsNotNull(options.Capabilities.Tools);
        Assert.IsNotNull(options.Handlers.ListToolsHandler);
        Assert.IsNotNull(options.Handlers.CallToolHandler);
    }

    [TestMethod]
    public void WithMcpEndpoint_ThrowsForInvalidArguments()
    {
        var services = new ServiceCollection();
        var builder = services.AddMcpServer();

        ExceptionAssert.Throws<ArgumentNullException>(
            () => McpEndpointToolsServerBuilderExtensions.WithMcpEndpoint(null!, "/domain1/mcp", TestTools.Domain1A));
        ExceptionAssert.Throws<ArgumentException>(
            () => builder.WithMcpEndpoint("", TestTools.Domain1A));
        ExceptionAssert.Throws<ArgumentNullException>(
            () => builder.WithMcpEndpoint("/domain1/mcp", null!));
    }
}
