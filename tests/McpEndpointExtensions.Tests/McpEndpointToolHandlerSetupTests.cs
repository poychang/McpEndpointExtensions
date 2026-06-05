using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpEndpointExtensions.Tests;

[TestClass]
public sealed class McpEndpointToolHandlerSetupTests
{
    [TestMethod]
    public async Task ListToolsHandler_ReturnsOnlyToolsForCurrentEndpoint()
    {
        using var provider = CreateServiceProvider();
        SetRequestPath(provider, "/domain1/mcp");
        var options = provider.GetRequiredService<IOptions<McpServerOptions>>().Value;

        var result = await options.Handlers.ListToolsHandler!(
            CreateListToolsContext(provider),
            CancellationToken.None);

        CollectionAssert.AreEquivalent(
            new[] { TestTools.Domain1A.ProtocolTool.Name, TestTools.Domain1B.ProtocolTool.Name },
            result.Tools.Select(tool => tool.Name).ToArray());
    }

    [TestMethod]
    public async Task ListToolsHandler_ReturnsEmptyListWhenHttpContextIsUnavailable()
    {
        using var provider = CreateServiceProvider();
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = null;
        var options = provider.GetRequiredService<IOptions<McpServerOptions>>().Value;

        var result = await options.Handlers.ListToolsHandler!(
            CreateListToolsContext(provider),
            CancellationToken.None);

        Assert.IsEmpty(result.Tools);
    }

    [TestMethod]
    public async Task CallToolHandler_RejectsToolNotAvailableOnCurrentEndpoint()
    {
        using var provider = CreateServiceProvider();
        SetRequestPath(provider, "/domain1/mcp");
        var options = provider.GetRequiredService<IOptions<McpServerOptions>>().Value;

        var exception = await ExceptionAssert.ThrowsAsync<McpProtocolException>(() =>
            options.Handlers.CallToolHandler!(
                CreateCallToolContext(provider, TestTools.Domain2B.ProtocolTool.Name, "hello"),
                CancellationToken.None).AsTask());

        Assert.AreEqual(McpErrorCode.InvalidParams, exception.ErrorCode);
        StringAssert.Contains(exception.Message, TestTools.Domain2B.ProtocolTool.Name);
    }

    [TestMethod]
    public async Task CallToolHandler_RejectsAllToolsWhenHttpContextIsUnavailable()
    {
        using var provider = CreateServiceProvider();
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = null;
        var options = provider.GetRequiredService<IOptions<McpServerOptions>>().Value;

        var exception = await ExceptionAssert.ThrowsAsync<McpProtocolException>(() =>
            options.Handlers.CallToolHandler!(
                CreateCallToolContext(provider, TestTools.Domain1A.ProtocolTool.Name, "hello"),
                CancellationToken.None).AsTask());

        Assert.AreEqual(McpErrorCode.InvalidParams, exception.ErrorCode);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        services
            .AddMcpServer()
            .WithMcpEndpoint("/domain1/mcp", TestTools.Domain1A, TestTools.Domain1B)
            .WithMcpEndpoint("/domain2/mcp", TestTools.Domain2B);

        return services.BuildServiceProvider();
    }

    private static void SetRequestPath(ServiceProvider provider, string path)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = provider,
        };
        context.Request.Path = path;

        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
    }

    private static RequestContext<ListToolsRequestParams> CreateListToolsContext(IServiceProvider services)
    {
        return new RequestContext<ListToolsRequestParams>(
            new TestMcpServer(services),
            new JsonRpcRequest
            {
                Id = new RequestId(1),
                Method = "tools/list",
            },
            new ListToolsRequestParams());
    }

    private static RequestContext<CallToolRequestParams> CreateCallToolContext(
        IServiceProvider services,
        string name,
        string input)
    {
        return new RequestContext<CallToolRequestParams>(
            new TestMcpServer(services),
            new JsonRpcRequest
            {
                Id = new RequestId(1),
                Method = "tools/call",
            },
            new CallToolRequestParams
            {
                Name = name,
                Arguments = new Dictionary<string, JsonElement>
                {
                    ["input"] = JsonSerializer.SerializeToElement(input),
                },
            });
    }

#pragma warning disable MCPEXP002
    private sealed class TestMcpServer(IServiceProvider services) : McpServer
    {
        public override ClientCapabilities ClientCapabilities { get; } = new();

        public override Implementation ClientInfo { get; } = new()
        {
            Name = "test-client",
            Version = "1.0",
        };

        public override McpServerOptions ServerOptions { get; } = new();

        public override IServiceProvider Services { get; } = services;

        public override LoggingLevel? LoggingLevel => null;

        public override string SessionId => "test-session";

        public override string NegotiatedProtocolVersion => "2025-11-25";

        public override Task RunAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task<JsonRpcResponse> SendRequestAsync(
            JsonRpcRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override Task SendMessageAsync(
            JsonRpcMessage message,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override IAsyncDisposable RegisterNotificationHandler(
            string method,
            Func<JsonRpcNotification, CancellationToken, ValueTask> handler)
        {
            return NoopAsyncDisposable.Instance;
        }

        public override ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
#pragma warning restore MCPEXP002

    private sealed class NoopAsyncDisposable : IAsyncDisposable
    {
        public static readonly NoopAsyncDisposable Instance = new();

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
