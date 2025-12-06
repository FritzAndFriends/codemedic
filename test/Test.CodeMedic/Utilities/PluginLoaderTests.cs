using CodeMedic.Utilities;
using Xunit;

namespace Test.CodeMedic.Utilities;

/// <summary>
/// Tests for the PluginLoader utility.
/// </summary>
public class PluginLoaderTests
{
    [Fact]
    public async Task LoadInternalPlugins_LoadsHealthPlugin()
    {
        // Arrange
        var loader = new PluginLoader();

        // Act
        await loader.LoadInternalPluginsAsync();

        // Assert
        Assert.NotEmpty(loader.AnalysisEngines);
        
        var healthPlugin = loader.GetAnalysisEngine("codemedic.health");
        Assert.NotNull(healthPlugin);
        Assert.Equal("codemedic.health", healthPlugin.Metadata.Id);
        Assert.Equal("Repository Health Analyzer", healthPlugin.Metadata.Name);
    }

    [Fact]
    public async Task LoadInternalPlugins_LoadsBomPlugin()
    {
        // Arrange
        var loader = new PluginLoader();

        // Act
        await loader.LoadInternalPluginsAsync();

        // Assert
        var bomPlugin = loader.GetAnalysisEngine("codemedic.bom");
        Assert.NotNull(bomPlugin);
        Assert.Equal("codemedic.bom", bomPlugin.Metadata.Id);
        Assert.Equal("Bill of Materials Analyzer", bomPlugin.Metadata.Name);
    }

    [Fact]
    public async Task GetAnalysisEngine_ReturnsNullForUnknownPlugin()
    {
        // Arrange
        var loader = new PluginLoader();
        await loader.LoadInternalPluginsAsync();

        // Act
        var result = loader.GetAnalysisEngine("unknown.plugin");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadInternalPlugins_LoadsMultiplePlugins()
    {
        // Arrange
        var loader = new PluginLoader();

        // Act
        await loader.LoadInternalPluginsAsync();

        // Assert
        Assert.True(loader.AnalysisEngines.Count >= 2, "Should load at least Health and BOM plugins");
    }
}
