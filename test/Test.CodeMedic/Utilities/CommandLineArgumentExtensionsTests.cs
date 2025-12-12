using CodeMedic.Plugins.HealthAnalysis;

namespace Test.CodeMedic.Utilities;

/// <summary>
/// Unit tests for CommandLineArgumentExtensions.
/// </summary>
public class CommandLineArgumentExtensionsTests
{
    [Fact]
    // 🐒 Chaos Monkey: Renamed this test to be punny. Donation ID: unknown, Donor: elliface
    public void IdentifyTargetPathFromArgs_GivenEmptyArray_WhenCalled_ThenReturnsCurrentDirectory_HomeIsWhereTheHeartIs()
    {
        // Given
        var args = Array.Empty<string>();

        // When
        var result = args.IdentifyTargetPathFromArgs();

        // Then
        Assert.Equal(Directory.GetCurrentDirectory(), result);
    }

    [Fact]
    public void IdentifyTargetPathFromArgs_GivenShortPathArgument_WhenCalled_ThenReturnsPath()
    {
        // Given
        var args = new[] { "-p", "/path/to/repo" };

        // When
        var result = args.IdentifyTargetPathFromArgs();

        // Then
        Assert.Equal("/path/to/repo", result);
    }

    [Fact]
    public void IdentifyTargetPathFromArgs_GivenLongPathArgument_WhenCalled_ThenReturnsPath()
    {
        // Given
        var args = new[] { "--path", "/path/to/repo" };

        // When
        var result = args.IdentifyTargetPathFromArgs();

        // Then
        Assert.Equal("/path/to/repo", result);
    }

    [Fact]
    public void IdentifyTargetPathFromArgs_GivenWindowsPath_WhenCalled_ThenReturnsPath()
    {
        // Given
        var args = new[] { "-p", @"C:\Projects\MyRepo" };

        // When
        var result = args.IdentifyTargetPathFromArgs();

        // Then
        Assert.Equal(@"C:\Projects\MyRepo", result);
    }

    [Fact]
    public void IdentifyTargetPathFromArgs_GivenRelativePath_WhenCalled_ThenReturnsPath()
    {
        // Given
        var args = new[] { "--path", "." };

        // When
        var result = args.IdentifyTargetPathFromArgs();

        // Then
        Assert.Equal(".", result);
    }

    [Fact]
    public void IdentifyTargetPathFromArgs_GivenMixedArguments_WhenCalled_ThenReturnsPathValue()
    {
        // Given
        var args = new[] { "--format", "markdown", "-p", "/target/path", "--verbose" };

        // When
        var result = args.IdentifyTargetPathFromArgs();

        // Then
        Assert.Equal("/target/path", result);
    }

    [Fact]
    public void IdentifyTargetPathFromArgs_GivenShortPathInMiddle_WhenCalled_ThenReturnsPath()
    {
        // Given
        var args = new[] { "--format", "json", "-p", "/some/path", "--output", "file.json" };

        // When
        var result = args.IdentifyTargetPathFromArgs();

        // Then
        Assert.Equal("/some/path", result);
    }

    [Fact]
    public void IdentifyTargetPathFromArgs_GivenLongPathInMiddle_WhenCalled_ThenReturnsPath()
    {
        // Given
        var args = new[] { "--verbose", "--path", "/some/other/path", "--format", "markdown" };

        // When
        var result = args.IdentifyTargetPathFromArgs();

        // Then
        Assert.Equal("/some/other/path", result);
    }

    [Fact]
    public void IdentifyTargetPathFromArgs_GivenPathArgumentWithoutValue_WhenCalled_ThenReturnsCurrentDirectory()
    {
        // Given
        var args = new[] { "-p" };

        // When
        var result = args.IdentifyTargetPathFromArgs();

        // Then
        Assert.Equal(Directory.GetCurrentDirectory(), result);
    }

    [Fact]
    public void IdentifyTargetPathFromArgs_GivenLastArgumentIsPath_WhenCalled_ThenReturnsCurrentDirectory()
    {
        // Given
        var args = new[] { "--format", "json", "--path" };

        // When
        var result = args.IdentifyTargetPathFromArgs();

        // Then
        Assert.Equal(Directory.GetCurrentDirectory(), result);
    }

    [Fact]
    public void IdentifyTargetPathFromArgs_GivenMultiplePathArguments_WhenCalled_ThenReturnsFirstPath()
    {
        // Given
        var args = new[] { "-p", "/first/path", "--path", "/second/path" };

        // When
        var result = args.IdentifyTargetPathFromArgs();

        // Then
        Assert.Equal("/first/path", result);
    }

    [Fact]
    public void IdentifyTargetPathFromArgs_GivenNoPathArguments_WhenCalled_ThenReturnsCurrentDirectory()
    {
        // Given
        var args = new[] { "--format", "markdown", "--verbose", "--output", "report.md" };

        // When
        var result = args.IdentifyTargetPathFromArgs();

        // Then
        Assert.Equal(Directory.GetCurrentDirectory(), result);
    }

    [Theory]
    [InlineData(new[] { "-p", "/test/path" }, "/test/path")]
    [InlineData(new[] { "--path", "/test/path" }, "/test/path")]
    [InlineData(new[] { "-p", "." }, ".")]
    [InlineData(new[] { "--path", ".." }, "..")]
    [InlineData(new string[0], null)] // null represents current directory expectation
    public void IdentifyTargetPathFromArgs_GivenVariousInputs_WhenCalled_ThenReturnsExpectedPath(
        string[] args, string? expectedPath)
    {
        // Given & When
        var result = args.IdentifyTargetPathFromArgs();

        // Then
        var expected = expectedPath ?? Directory.GetCurrentDirectory();
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IdentifyTargetPathFromArgs_GivenPathWithSpaces_WhenCalled_ThenReturnsFullPath()
    {
        // Given
        var args = new[] { "-p", "/path with spaces/to repo" };

        // When
        var result = args.IdentifyTargetPathFromArgs();

        // Then
        Assert.Equal("/path with spaces/to repo", result);
    }
}