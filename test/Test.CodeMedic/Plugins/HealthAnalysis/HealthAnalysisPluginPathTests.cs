using CodeMedic.Abstractions.Plugins;
using CodeMedic.Plugins.HealthAnalysis;
using Moq;
using CodeMedic.Abstractions;

namespace Test.CodeMedic.Plugins.HealthAnalysis;

/// <summary>
/// Unit tests for HealthAnalysisPlugin path argument functionality.
/// </summary>
public class HealthAnalysisPluginPathTests
{
    private readonly HealthAnalysisPlugin _plugin;

    public HealthAnalysisPluginPathTests()
    {
        _plugin = new HealthAnalysisPlugin();
    }

    [Fact]
    public void RegisterCommands_WhenCalled_ThenReturnsCommandWithPathArgument()
    {
        // When
        var commands = _plugin.RegisterCommands();

        // Then
        Assert.NotNull(commands);
        Assert.Single(commands);
        
        var command = commands[0];
        Assert.Equal("health", command.Name);
        Assert.Equal("Display repository health dashboard", command.Description);
        
        Assert.NotNull(command.Arguments);
        Assert.Single(command.Arguments);
        
        var pathArg = command.Arguments[0];
        Assert.Equal("Path to the repository to analyze", pathArg.Description);
        Assert.Equal("p", pathArg.ShortName);
        Assert.Equal("path", pathArg.LongName);
        Assert.False(pathArg.IsRequired);
        Assert.True(pathArg.HasValue);
        Assert.Equal("current directory", pathArg.DefaultValue);
        Assert.Equal("path", pathArg.ValueName);
    }

    [Fact]
    public void RegisterCommands_WhenCalled_ThenReturnsCommandWithCorrectExamples()
    {
        // When
        var commands = _plugin.RegisterCommands();

        // Then
        var command = commands![0];
        Assert.NotNull(command.Examples);
        Assert.Contains("codemedic health", command.Examples);
        Assert.Contains("codemedic health -p /path/to/repo", command.Examples);
        Assert.Contains("codemedic health --path /path/to/repo --format markdown", command.Examples);
        Assert.Contains("codemedic health --format md > report.md", command.Examples);
    }

    [Fact]
    public async Task ExecuteHealthCommandAsync_GivenEmptyArgs_WhenCalled_ThenUsesCurrentDirectory()
    {
        // Given
        var mockRenderer = new Mock<IRenderer>();
        var command = _plugin.RegisterCommands()![0];
        var args = Array.Empty<string>();

        // Setup the renderer to avoid actual rendering
        mockRenderer.Setup(r => r.RenderBanner()).Verifiable();
        mockRenderer.Setup(r => r.RenderSectionHeader(It.IsAny<string>())).Verifiable();
        mockRenderer.Setup(r => r.RenderWaitAsync(It.IsAny<string>(), It.IsAny<Func<Task>>()))
            .Returns<string, Func<Task>>((_, action) => action())
            .Verifiable();
        mockRenderer.Setup(r => r.RenderReport(It.IsAny<object>())).Verifiable();

        // When & Then - Should not throw and should call renderer methods
        var result = await command.Handler(args, mockRenderer.Object);
        
        // Verify renderer was called appropriately
        mockRenderer.Verify(r => r.RenderBanner(), Times.Once);
        mockRenderer.Verify(r => r.RenderSectionHeader("Repository Health Dashboard"), Times.Once);
        mockRenderer.Verify(r => r.RenderWaitAsync(
            It.Is<string>(s => s.Contains("Repository health and code quality analysis")), 
            It.IsAny<Func<Task>>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteHealthCommandAsync_GivenShortPathArg_WhenCalled_ThenUsesSpecifiedPath()
    {
        // Given
        var mockRenderer = new Mock<IRenderer>();
        var command = _plugin.RegisterCommands()![0];
        var testPath = Path.GetTempPath();
        var args = new[] { "-p", testPath };

        // Setup the renderer
        mockRenderer.Setup(r => r.RenderBanner()).Verifiable();
        mockRenderer.Setup(r => r.RenderSectionHeader(It.IsAny<string>())).Verifiable();
        mockRenderer.Setup(r => r.RenderWaitAsync(It.IsAny<string>(), It.IsAny<Func<Task>>()))
            .Returns<string, Func<Task>>((_, action) => action())
            .Verifiable();
        mockRenderer.Setup(r => r.RenderReport(It.IsAny<object>())).Verifiable();

        // When
        var result = await command.Handler(args, mockRenderer.Object);

        // Then - Should complete without throwing
        mockRenderer.Verify(r => r.RenderBanner(), Times.Once);
        mockRenderer.Verify(r => r.RenderSectionHeader("Repository Health Dashboard"), Times.Once);
    }

    [Fact]
    public async Task ExecuteHealthCommandAsync_GivenLongPathArg_WhenCalled_ThenUsesSpecifiedPath()
    {
        // Given
        var mockRenderer = new Mock<IRenderer>();
        var command = _plugin.RegisterCommands()![0];
        var testPath = Path.GetTempPath();
        var args = new[] { "--path", testPath };

        // Setup the renderer
        mockRenderer.Setup(r => r.RenderBanner()).Verifiable();
        mockRenderer.Setup(r => r.RenderSectionHeader(It.IsAny<string>())).Verifiable();
        mockRenderer.Setup(r => r.RenderWaitAsync(It.IsAny<string>(), It.IsAny<Func<Task>>()))
            .Returns<string, Func<Task>>((_, action) => action())
            .Verifiable();
        mockRenderer.Setup(r => r.RenderReport(It.IsAny<object>())).Verifiable();

        // When
        var result = await command.Handler(args, mockRenderer.Object);

        // Then - Should complete without throwing
        mockRenderer.Verify(r => r.RenderBanner(), Times.Once);
        mockRenderer.Verify(r => r.RenderSectionHeader("Repository Health Dashboard"), Times.Once);
    }

    [Fact]
    public async Task ExecuteHealthCommandAsync_GivenCurrentDirectoryPath_WhenCalled_ThenProcessesSuccessfully()
    {
        // Given
        var mockRenderer = new Mock<IRenderer>();
        var command = _plugin.RegisterCommands()![0];
        var args = new[] { "-p", "." };

        // Setup minimal renderer mock
        mockRenderer.Setup(r => r.RenderBanner());
        mockRenderer.Setup(r => r.RenderSectionHeader(It.IsAny<string>()));
        mockRenderer.Setup(r => r.RenderWaitAsync(It.IsAny<string>(), It.IsAny<Func<Task>>()))
            .Returns<string, Func<Task>>((_, action) => action());
        mockRenderer.Setup(r => r.RenderReport(It.IsAny<object>()));

        // When & Then - Should not throw
        var result = await command.Handler(args, mockRenderer.Object);
        
        // Verify basic renderer calls were made
        mockRenderer.Verify(r => r.RenderBanner(), Times.Once);
        mockRenderer.Verify(r => r.RenderSectionHeader("Repository Health Dashboard"), Times.Once);
    }

    [Fact]
    public async Task ExecuteHealthCommandAsync_GivenParentDirectoryPath_WhenCalled_ThenProcessesSuccessfully()
    {
        // Given
        var mockRenderer = new Mock<IRenderer>();
        var command = _plugin.RegisterCommands()![0];
        var args = new[] { "--path", ".." };

        // Setup minimal renderer mock
        mockRenderer.Setup(r => r.RenderBanner());
        mockRenderer.Setup(r => r.RenderSectionHeader(It.IsAny<string>()));
        mockRenderer.Setup(r => r.RenderWaitAsync(It.IsAny<string>(), It.IsAny<Func<Task>>()))
            .Returns<string, Func<Task>>((_, action) => action());
        mockRenderer.Setup(r => r.RenderReport(It.IsAny<object>()));

        // When & Then - Should not throw
        var result = await command.Handler(args, mockRenderer.Object);
        
        // Verify basic renderer calls were made
        mockRenderer.Verify(r => r.RenderBanner(), Times.Once);
        mockRenderer.Verify(r => r.RenderSectionHeader("Repository Health Dashboard"), Times.Once);
    }

    [Fact]
    public void Metadata_WhenAccessed_ThenReturnsCorrectInformation()
    {
        // When
        var metadata = _plugin.Metadata;

        // Then
        Assert.Equal("codemedic.health", metadata.Id);
        Assert.Equal("Repository Health Analyzer", metadata.Name);
        Assert.Equal("Analyzes .NET repository health, including projects, dependencies, and code quality indicators", metadata.Description);
        Assert.Equal("CodeMedic Team", metadata.Author);
        Assert.NotNull(metadata.Tags);
        Assert.Contains("health", metadata.Tags);
        Assert.Contains("analysis", metadata.Tags);
        Assert.Contains("repository", metadata.Tags);
        Assert.Contains("dotnet", metadata.Tags);
    }

    [Fact]
    public void AnalysisDescription_WhenAccessed_ThenReturnsCorrectDescription()
    {
        // When
        var description = _plugin.AnalysisDescription;

        // Then
        Assert.Equal("Repository health and code quality analysis", description);
    }

    [Fact] 
    public async Task InitializeAsync_WhenCalled_ThenCompletesSuccessfully()
    {
        // When & Then - Should complete without throwing
        await _plugin.InitializeAsync();
    }
}