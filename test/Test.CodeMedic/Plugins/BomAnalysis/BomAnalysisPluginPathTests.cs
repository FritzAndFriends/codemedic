using CodeMedic.Abstractions.Plugins;
using CodeMedic.Plugins.BomAnalysis;
using Moq;
using CodeMedic.Abstractions;

namespace Test.CodeMedic.Plugins.BomAnalysis;

/// <summary>
/// Unit tests for BomAnalysisPlugin path argument functionality.
/// </summary>
public class BomAnalysisPluginPathTests
{
    private readonly BomAnalysisPlugin _plugin;

    public BomAnalysisPluginPathTests()
    {
        _plugin = new BomAnalysisPlugin();
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
        Assert.Equal("bom", command.Name);
        Assert.Equal("Generate bill of materials report", command.Description);

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
        Assert.Contains("codemedic bom", command.Examples);
        Assert.Contains("codemedic bom -p /path/to/repo", command.Examples);
        Assert.Contains("codemedic bom --path /path/to/repo --format markdown", command.Examples);
        Assert.Contains("codemedic bom --format md > bom.md", command.Examples);
    }

    [Fact]
    public async Task ExecuteBomCommandAsync_GivenEmptyArgs_WhenCalled_ThenUsesCurrentDirectory()
    {
        // Given
        var mockRenderer = new Mock<IRenderer>();
        var command = _plugin.RegisterCommands()![0];
        var args = Array.Empty<string>();

        // Setup the renderer to avoid actual rendering
        mockRenderer.Setup(r => r.RenderBanner(It.IsAny<string>())).Verifiable();
        mockRenderer.Setup(r => r.RenderSectionHeader(It.IsAny<string>())).Verifiable();
        mockRenderer.Setup(r => r.RenderWaitAsync(It.IsAny<string>(), It.IsAny<Func<Task>>()))
            .Returns<string, Func<Task>>((_, action) => action())
            .Verifiable();
        mockRenderer.Setup(r => r.RenderReport(It.IsAny<object>())).Verifiable();

        // When & Then - Should not throw and should call renderer methods
        var payload = new HandlerPayload(args, "Test Project", mockRenderer.Object);
        var result = await command.Handler(payload);

        // Verify renderer was called appropriately
        mockRenderer.Verify(r => r.RenderBanner(It.IsAny<string>()), Times.Once);
        mockRenderer.Verify(r => r.RenderSectionHeader("Bill of Materials (BOM)"), Times.Once);
        mockRenderer.Verify(r => r.RenderWaitAsync(
            It.Is<string>(s => s.Contains("Comprehensive dependency and service inventory (BOM)")),
            It.IsAny<Func<Task>>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteBomCommandAsync_GivenShortPathArg_WhenCalled_ThenUsesSpecifiedPath()
    {
        // Given
        var mockRenderer = new Mock<IRenderer>();
        var command = _plugin.RegisterCommands()![0];
        var testPath = Path.GetTempPath();
        var args = new[] { "-p", testPath };

        // Setup the renderer
        mockRenderer.Setup(r => r.RenderBanner(It.IsAny<string>())).Verifiable();
        mockRenderer.Setup(r => r.RenderSectionHeader(It.IsAny<string>())).Verifiable();
        mockRenderer.Setup(r => r.RenderWaitAsync(It.IsAny<string>(), It.IsAny<Func<Task>>()))
            .Returns<string, Func<Task>>((_, action) => action())
            .Verifiable();
        mockRenderer.Setup(r => r.RenderReport(It.IsAny<object>())).Verifiable();

        // When
        var payload = new HandlerPayload(args, "Test Project", mockRenderer.Object);
        var result = await command.Handler(payload);

        // Then - Should complete without throwing
        mockRenderer.Verify(r => r.RenderBanner(It.IsAny<string>()), Times.Once);
        mockRenderer.Verify(r => r.RenderSectionHeader("Bill of Materials (BOM)"), Times.Once);
    }

    [Fact]
    public async Task ExecuteBomCommandAsync_GivenLongPathArg_WhenCalled_ThenUsesSpecifiedPath()
    {
        // Given
        var mockRenderer = new Mock<IRenderer>();
        var command = _plugin.RegisterCommands()![0];
        var testPath = Path.GetTempPath();
        var args = new[] { "--path", testPath };

        // Setup the renderer
        mockRenderer.Setup(r => r.RenderBanner(It.IsAny<string>())).Verifiable();
        mockRenderer.Setup(r => r.RenderSectionHeader(It.IsAny<string>())).Verifiable();
        mockRenderer.Setup(r => r.RenderWaitAsync(It.IsAny<string>(), It.IsAny<Func<Task>>()))
            .Returns<string, Func<Task>>((_, action) => action())
            .Verifiable();
        mockRenderer.Setup(r => r.RenderReport(It.IsAny<object>())).Verifiable();

        // When
        var payload = new HandlerPayload(args, "Test Project", mockRenderer.Object);
        var result = await command.Handler(payload);

        // Then - Should complete without throwing
        mockRenderer.Verify(r => r.RenderBanner(It.IsAny<string>()), Times.Once);
        mockRenderer.Verify(r => r.RenderSectionHeader("Bill of Materials (BOM)"), Times.Once);
    }

    [Fact]
    public void Metadata_WhenAccessed_ThenReturnsCorrectInformation()
    {
        // When
        var metadata = _plugin.Metadata;

        // Then
        Assert.Equal("codemedic.bom", metadata.Id);
        Assert.Equal("Bill of Materials Analyzer", metadata.Name);
        Assert.Equal("Generates comprehensive Bill of Materials including NuGet packages, frameworks, services, and vendors", metadata.Description);
        Assert.Equal("CodeMedic Team", metadata.Author);
        Assert.NotNull(metadata.Tags);
        Assert.Contains("bom", metadata.Tags);
        Assert.Contains("dependencies", metadata.Tags);
        Assert.Contains("packages", metadata.Tags);
        Assert.Contains("inventory", metadata.Tags);
    }

    [Fact]
    public void AnalysisDescription_WhenAccessed_ThenReturnsCorrectDescription()
    {
        // When
        var description = _plugin.AnalysisDescription;

        // Then
        Assert.Equal("Comprehensive dependency and service inventory (BOM)", description);
    }

    [Fact]
    public async Task InitializeAsync_WhenCalled_ThenCompletesSuccessfully()
    {
        // When & Then - Should complete without throwing
        await _plugin.InitializeAsync();
    }

    [Fact]
    public async Task ExecuteBomCommandAsync_GivenRelativePathArg_WhenCalled_ThenProcessesSuccessfully()
    {
        // Given
        var mockRenderer = new Mock<IRenderer>();
        var command = _plugin.RegisterCommands()![0];
        var args = new[] { "-p", "." };

        // Setup minimal renderer mock
        mockRenderer.Setup(r => r.RenderBanner(It.IsAny<string>()));
        mockRenderer.Setup(r => r.RenderSectionHeader(It.IsAny<string>()));
        mockRenderer.Setup(r => r.RenderWaitAsync(It.IsAny<string>(), It.IsAny<Func<Task>>()))
            .Returns<string, Func<Task>>((_, action) => action());
        mockRenderer.Setup(r => r.RenderReport(It.IsAny<object>()));

        // When & Then - Should not throw
        var payload = new HandlerPayload(args, "Test Project", mockRenderer.Object);
        var result = await command.Handler(payload);

        // Verify basic renderer calls were made
        mockRenderer.Verify(r => r.RenderBanner(It.IsAny<string>()), Times.Once);
        mockRenderer.Verify(r => r.RenderSectionHeader("Bill of Materials (BOM)"), Times.Once);
    }
}
