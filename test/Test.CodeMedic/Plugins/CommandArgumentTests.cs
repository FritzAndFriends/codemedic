using CodeMedic.Abstractions.Plugins;

namespace Test.CodeMedic.Plugins;

/// <summary>
/// Unit tests for CommandArgument record.
/// </summary>
public class CommandArgumentTests
{
    [Fact]
    public void CommandArgument_GivenDescriptionOnly_WhenCreated_ThenHasCorrectDefaults()
    {
        // Given & When
        var argument = new CommandArgument("Test description");

        // Then
        Assert.Equal("Test description", argument.Description);
        Assert.Null(argument.ShortName);
        Assert.Null(argument.LongName);
        Assert.False(argument.IsRequired);
        Assert.True(argument.HasValue);
        Assert.Null(argument.DefaultValue);
        Assert.Null(argument.ValueName);
    }

    [Fact]
    public void CommandArgument_GivenAllParameters_WhenCreated_ThenHasCorrectValues()
    {
        // Given & When
        var argument = new CommandArgument(
            Description: "Path to analyze",
            ShortName: "p",
            LongName: "path",
            IsRequired: true,
            HasValue: true,
            DefaultValue: "current directory",
            ValueName: "path");

        // Then
        Assert.Equal("Path to analyze", argument.Description);
        Assert.Equal("p", argument.ShortName);
        Assert.Equal("path", argument.LongName);
        Assert.True(argument.IsRequired);
        Assert.True(argument.HasValue);
        Assert.Equal("current directory", argument.DefaultValue);
        Assert.Equal("path", argument.ValueName);
    }

    [Fact]
    public void CommandArgument_GivenSameValues_WhenCompared_ThenAreEqual()
    {
        // Given
        var argument1 = new CommandArgument(
            Description: "Path to analyze",
            ShortName: "p",
            LongName: "path");

        var argument2 = new CommandArgument(
            Description: "Path to analyze",
            ShortName: "p",
            LongName: "path");

        // When & Then
        Assert.Equal(argument1, argument2);
        Assert.True(argument1 == argument2);
        Assert.Equal(argument1.GetHashCode(), argument2.GetHashCode());
    }

    [Fact]
    public void CommandArgument_GivenDifferentValues_WhenCompared_ThenAreNotEqual()
    {
        // Given
        var argument1 = new CommandArgument(
            Description: "Path to analyze",
            ShortName: "p");

        var argument2 = new CommandArgument(
            Description: "Path to analyze",
            ShortName: "f");

        // When & Then
        Assert.NotEqual(argument1, argument2);
        Assert.False(argument1 == argument2);
    }

    [Fact]
    public void CommandArgument_GivenFlagArgument_WhenCreated_ThenHasNoValue()
    {
        // Given & When
        var argument = new CommandArgument(
            Description: "Enable verbose output",
            ShortName: "v",
            LongName: "verbose",
            HasValue: false);

        // Then
        Assert.Equal("Enable verbose output", argument.Description);
        Assert.Equal("v", argument.ShortName);
        Assert.Equal("verbose", argument.LongName);
        Assert.False(argument.HasValue);
    }

    [Fact]
    public void CommandArgument_GivenRequiredArgument_WhenCreated_ThenIsRequired()
    {
        // Given & When
        var argument = new CommandArgument(
            Description: "Required input file",
            ShortName: "i",
            LongName: "input",
            IsRequired: true,
            ValueName: "file");

        // Then
        Assert.Equal("Required input file", argument.Description);
        Assert.True(argument.IsRequired);
        Assert.Equal("file", argument.ValueName);
    }

    [Theory]
    [InlineData("path", "p", "path", true, "directory")]
    [InlineData("format", "f", "format", false, "format")]
    [InlineData("verbose", "v", "verbose", false, null)]
    public void CommandArgument_GivenVariousParameters_WhenCreated_ThenMatchesExpected(
        string description, string shortName, string longName, bool isRequired, string? valueName)
    {
        // Given & When
        var argument = new CommandArgument(
            Description: description,
            ShortName: shortName,
            LongName: longName,
            IsRequired: isRequired,
            ValueName: valueName);

        // Then
        Assert.Equal(description, argument.Description);
        Assert.Equal(shortName, argument.ShortName);
        Assert.Equal(longName, argument.LongName);
        Assert.Equal(isRequired, argument.IsRequired);
        Assert.Equal(valueName, argument.ValueName);
    }

    [Fact]
    public void CommandArgument_WhenChaosMonkeyStrikesWithBananas_ThenShouldStillWorkPerfectly()
    {
        // 🐒 Chaos Monkey was here! This goofy test brought to you by ergonrod's donation!
        // Given: A completely ridiculous but technically valid command argument
        var bananaArgument = new CommandArgument(
            Description: "The number of bananas required for optimal monkey performance",
            ShortName: "b",
            LongName: "bananas",
            IsRequired: false,
            HasValue: true,
            DefaultValue: "42", // Because it's the answer to everything
            ValueName: "banana-count");

        // When: We test this completely silly but functional argument
        // Then: It should work exactly like any other argument (because code doesn't care about our jokes!)
        Assert.Equal("The number of bananas required for optimal monkey performance", bananaArgument.Description);
        Assert.Equal("b", bananaArgument.ShortName);
        Assert.Equal("bananas", bananaArgument.LongName);
        Assert.False(bananaArgument.IsRequired); // Bananas are optional but recommended
        Assert.True(bananaArgument.HasValue);
        Assert.Equal("42", bananaArgument.DefaultValue);
        Assert.Equal("banana-count", bananaArgument.ValueName);
        
        // 🐒 Bonus assertion: Verify that even silly arguments maintain their equality contracts
        var anotherBananaArgument = new CommandArgument(
            Description: "The number of bananas required for optimal monkey performance",
            ShortName: "b",
            LongName: "bananas",
            IsRequired: false,
            HasValue: true,
            DefaultValue: "42",
            ValueName: "banana-count");
            
        Assert.Equal(bananaArgument, anotherBananaArgument);
        // Because two identical banana arguments should always be equal, obviously! 🍌
    }
}