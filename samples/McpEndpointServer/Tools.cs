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

[McpServerToolType]
public sealed class Domain2Tools
{
    [McpServerTool, Description("Echoes B1 calling.")]
    public static string B1(string input) => $"B1: {input}";
}
