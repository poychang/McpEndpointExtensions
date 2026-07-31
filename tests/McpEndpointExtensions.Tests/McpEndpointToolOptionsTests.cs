using Microsoft.AspNetCore.Http;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

namespace McpEndpointExtensions.Tests;

[TestClass]
public sealed class McpEndpointToolOptionsTests
{
    [TestMethod]
    public void MapToolsToEndpoint_StoresRegisteredPatterns()
    {
        var options = new McpEndpointToolOptions();

        options.MapToolsToEndpoint("/domain1/mcp", [TestTools.Domain1A]);
        options.MapToolsToEndpoint("/domain2/mcp", [TestTools.Domain2B]);

        CollectionAssert.AreEquivalent(
            new[] { "/domain1/mcp", "/domain2/mcp" },
            options.Patterns.ToArray());
    }

    [TestMethod]
    public void MapToolsToEndpoint_ReplacesExistingToolsForSamePatternIgnoringCase()
    {
        var options = new McpEndpointToolOptions();

        options.MapToolsToEndpoint("/Domain/Mcp", [TestTools.Domain1A]);
        options.MapToolsToEndpoint("/domain/mcp", [TestTools.Domain2B]);

        var tools = options.GetToolsForPath(new PathString("/domain/mcp"));

        Assert.HasCount(1, options.Patterns);
        Assert.HasCount(1, tools);
        Assert.AreSame(TestTools.Domain2B, tools[0]);
    }

    [TestMethod]
    public void GetToolsForPath_ReturnsToolsForMatchingPathPrefix()
    {
        var options = new McpEndpointToolOptions();
        options.MapToolsToEndpoint("/domain1/mcp", [TestTools.Domain1A, TestTools.Domain1B]);

        var tools = options.GetToolsForPath(new PathString("/domain1/mcp/messages"));

        CollectionAssert.AreEqual(
            new[] { TestTools.Domain1A, TestTools.Domain1B },
            tools.ToArray());
    }

    [TestMethod]
    public void GetToolsForPath_ReturnsEmptyListForUnknownPath()
    {
        var options = new McpEndpointToolOptions();
        options.MapToolsToEndpoint("/domain1/mcp", [TestTools.Domain1A]);

        var tools = options.GetToolsForPath(new PathString("/domain2/mcp"));

        Assert.IsEmpty(tools);
    }

    [TestMethod]
    public void GetToolsForPath_ReturnsToolsForLongestMatchingPrefix()
    {
        var options = new McpEndpointToolOptions();
        options.MapToolsToEndpoint("/domain1/mcp", [TestTools.Domain1A]);
        options.MapToolsToEndpoint("/domain1/mcp/admin", [TestTools.Domain1B]);

        var tools = options.GetToolsForPath(new PathString("/domain1/mcp/admin/messages"));

        CollectionAssert.AreEqual(
            new[] { TestTools.Domain1B },
            tools.ToArray());
    }

    [TestMethod]
    public void MapToolsToEndpoint_ThrowsForInvalidArguments()
    {
        var options = new McpEndpointToolOptions();

        ExceptionAssert.Throws<ArgumentException>(() => options.MapToolsToEndpoint("", [TestTools.Domain1A]));
        ExceptionAssert.Throws<ArgumentNullException>(() => options.MapToolsToEndpoint("/domain1/mcp", null!));
    }
}
