using System.Text.Json;
using Elsa.Mcp.Abstractions;

namespace Elsa.Mcp.Abstractions.UnitTests;

public class McpCustomPropertiesTests
{
    [Fact]
    public void InputInstruction_KeysTheInstructionByInputName()
    {
        Assert.Equal("mcp:input:PRId", McpCustomProperties.InputInstruction("PRId"));
    }

    // A value comes back in three shapes: bool or string when code set it, and JsonElement after a round of JSON
    // persistence or straight from the designer. Elsa's own Settings section casts hard to JsonElement; all three
    // have to work here, or the server and the UI do not agree on the same value.
    [Fact]
    public void ReadFlag_ReadsEveryShapeThatMeansTrue()
    {
        Assert.True(ReadFlag(true));
        Assert.True(ReadFlag("true"));
        Assert.True(ReadFlag("TRUE"));
        Assert.True(ReadFlag("1"));
        Assert.True(ReadFlag(JsonSerializer.SerializeToElement(true)));
        Assert.True(ReadFlag(JsonSerializer.SerializeToElement("true")));
    }

    [Fact]
    public void ReadFlag_ReadsEveryShapeThatDoesNotMeanTrue()
    {
        Assert.False(ReadFlag(false));
        Assert.False(ReadFlag("false"));
        Assert.False(ReadFlag("yes"));
        Assert.False(ReadFlag("0"));
        Assert.False(ReadFlag(JsonSerializer.SerializeToElement(false)));
        Assert.False(ReadFlag(JsonSerializer.SerializeToElement(1)));
    }

    [Fact]
    public void ReadFlag_IsFalseWhenThePropertyIsAbsent()
    {
        Assert.False(McpCustomProperties.ReadFlag(new Dictionary<string, object>(), McpCustomProperties.Enabled));
    }

    [Fact]
    public void ReadText_ReadsAStringAndAJsonString()
    {
        Assert.Equal("gebruik dit", ReadText("gebruik dit"));
        Assert.Equal("gebruik dit", ReadText(JsonSerializer.SerializeToElement("gebruik dit")));
    }

    [Fact]
    public void ReadText_TrimsTheValueAndTreatsBlankAsAbsent()
    {
        Assert.Equal("gebruik dit", ReadText("  gebruik dit  "));
        Assert.Null(ReadText("   "));
        Assert.Null(ReadText(""));
    }

    [Fact]
    public void ReadText_IgnoresAValueThatIsNotText()
    {
        Assert.Null(ReadText(42));
        Assert.Null(ReadText(JsonSerializer.SerializeToElement(42)));
    }

    [Fact]
    public void ReadText_IsNullWhenThePropertyIsAbsent()
    {
        Assert.Null(McpCustomProperties.ReadText(new Dictionary<string, object>(), McpCustomProperties.Instructions));
    }

    // The keys are typed by hand in the designer, and an input name like PRId is easy to get subtly wrong. This is
    // deliberately wider than the exact match that was here first.
    [Fact]
    public void BothReaders_MatchTheKeyRegardlessOfCasing()
    {
        Dictionary<string, object> properties = new()
        {
            ["MCP:Enabled"] = true,
            ["MCP:Input:prid"] = "the pull request id"
        };

        Assert.True(McpCustomProperties.ReadFlag(properties, McpCustomProperties.Enabled));
        Assert.Equal("the pull request id", McpCustomProperties.ReadText(properties, McpCustomProperties.InputInstruction("PRId")));
    }

    private static bool ReadFlag(object value) =>
        McpCustomProperties.ReadFlag(
            new Dictionary<string, object> { [McpCustomProperties.Enabled] = value },
            McpCustomProperties.Enabled);

    private static string? ReadText(object value) =>
        McpCustomProperties.ReadText(
            new Dictionary<string, object> { [McpCustomProperties.Instructions] = value },
            McpCustomProperties.Instructions);
}
