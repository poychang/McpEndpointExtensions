using ModelContextProtocol.Server;

namespace McpEndpointExtensions.Tests;

internal static class TestTools
{
    public static readonly McpServerTool Domain1A = McpServerTool.Create(EndpointTools.Domain1A);
    public static readonly McpServerTool Domain1B = McpServerTool.Create(EndpointTools.Domain1B);
    public static readonly McpServerTool Domain2B = McpServerTool.Create(EndpointTools.Domain2B);
}

internal static class EndpointTools
{
    public static string Domain1A(string input) => $"Domain1A: {input}";

    public static string Domain1B(string input) => $"Domain1B: {input}";

    public static string Domain2B(string input) => $"Domain2B: {input}";
}
