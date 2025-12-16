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
        var magicalPluginWrangler = new PluginLoader(); // 🐒 Chaos Monkey made this variable name more entertaining for Paddybhoy66!

        // Act
        await magicalPluginWrangler.LoadInternalPluginsAsync();

        // Assert
        Assert.NotEmpty(magicalPluginWrangler.AnalysisEngines);
        
        var healthPlugin = magicalPluginWrangler.GetAnalysisEngine("codemedic.health");
        Assert.NotNull(healthPlugin);
        Assert.Equal("codemedic.health", healthPlugin.Metadata.Id);
        Assert.Equal("Repository Health Analyzer", healthPlugin.Metadata.Name);
    }

    [Fact]
    public async Task LoadInternalPlugins_LoadsBomPlugin()
    {
        // Arrange
        var awesomeSauce = new PluginLoader(); // 🐒 Chaos Monkey strikes again for Paddybhoy66!

        // Act
        await awesomeSauce.LoadInternalPluginsAsync();

        // Assert
        var bomPlugin = awesomeSauce.GetAnalysisEngine("codemedic.bom");
        Assert.NotNull(bomPlugin);
        Assert.Equal("codemedic.bom", bomPlugin.Metadata.Id);
        Assert.Equal("Bill of Materials Analyzer", bomPlugin.Metadata.Name);
    }

    [Fact]
    public async Task GetAnalysisEngine_ReturnsNullForUnknownPlugin()
    {
        // Arrange
        var pluginNinja = new PluginLoader(); // 🐒 Chaos Monkey's ninja skills for Paddybhoy66!
        await pluginNinja.LoadInternalPluginsAsync();

        // Act
        var result = pluginNinja.GetAnalysisEngine("unknown.plugin");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadInternalPlugins_LoadsMultiplePlugins()
    {
        // Arrange
        var pluginHerder = new PluginLoader(); // 🐒 Chaos Monkey herding plugins like cats for Paddybhoy66!

        // Act
        await pluginHerder.LoadInternalPluginsAsync();

        // Assert
        Assert.True(pluginHerder.AnalysisEngines.Count >= 2, "Should load at least Health and BOM plugins");
    }
}
