using CodeMedic.Commands;
using CodeMedic.Utilities;
using Moq;
using Xunit;

namespace Test.CodeMedic.Commands;

/// <summary>
/// Tests for the ConfigurationCommandHandler.
/// </summary>
public class ConfigurationCommandHandlerTests : IDisposable
{
	private readonly string _testDirectory;
	private readonly Mock<PluginLoader> _mockPluginLoader;

	public ConfigurationCommandHandlerTests()
	{
		// Create a temporary test directory
		_testDirectory = Path.Combine(Path.GetTempPath(), $"CodeMedic_Test_{Guid.NewGuid()}");
		Directory.CreateDirectory(_testDirectory);

		// Setup mock plugin loader
		_mockPluginLoader = new Mock<PluginLoader>();
	}

	public void Dispose()
	{
		// Clean up test directory
		if (Directory.Exists(_testDirectory))
		{
			Directory.Delete(_testDirectory, recursive: true);
		}
	}

	[Fact]
	public async Task HandleConfigurationFileAsync_WithValidJsonConfig_ReturnsSuccess()
	{
		// Arrange
		var testRepoPath = Path.Combine(_testDirectory, "test-repo");
		var outputPath = Path.Combine(_testDirectory, "output");
		Directory.CreateDirectory(testRepoPath);

		var configPath = Path.Combine(_testDirectory, "config.json");
		var jsonConfig = $$"""
		{
			"global": {
				"format": "markdown",
				"output-dir": "{{outputPath.Replace("\\", "\\\\")}}"
			},
			"repositories": [
				{
					"name": "TestRepo",
					"path": "{{testRepoPath.Replace("\\", "\\\\")}}",
					"commands": ["health", "bom"]
				}
			]
		}
		""";
		File.WriteAllText(configPath, jsonConfig);

		var pluginLoader = new PluginLoader();
		await pluginLoader.LoadInternalPluginsAsync();
		var handler = new ConfigurationCommandHandler(pluginLoader);

		// Act
		var result = await handler.HandleConfigurationFileAsync(configPath);

		// Assert
		Assert.Equal(0, result);
		Assert.True(Directory.Exists(outputPath));
		Assert.True(File.Exists(Path.Combine(outputPath, "TestRepo_health.md")));
		Assert.True(File.Exists(Path.Combine(outputPath, "TestRepo_bom.md")));
	}

	[Fact]
	public async Task HandleConfigurationFileAsync_WithValidYamlConfig_ReturnsSuccess()
	{
		// Arrange
		var testRepoPath = Path.Combine(_testDirectory, "test-repo");
		var outputPath = Path.Combine(_testDirectory, "output");
		Directory.CreateDirectory(testRepoPath);

		var configPath = Path.Combine(_testDirectory, "config.yaml");
		var yamlConfig = $"""
		global:
		  format: markdown
		  output-dir: {outputPath}
		repositories:
		  - name: TestRepo
		    path: {testRepoPath}
		    commands:
		      - health
		      - bom
		""";
		File.WriteAllText(configPath, yamlConfig);

		var pluginLoader = new PluginLoader();
		await pluginLoader.LoadInternalPluginsAsync();
		var handler = new ConfigurationCommandHandler(pluginLoader);

		// Act
		var result = await handler.HandleConfigurationFileAsync(configPath);

		// Assert
		Assert.Equal(0, result);
		Assert.True(Directory.Exists(outputPath));
		Assert.True(File.Exists(Path.Combine(outputPath, "TestRepo_health.md")));
		Assert.True(File.Exists(Path.Combine(outputPath, "TestRepo_bom.md")));
	}

	[Fact]
	public async Task HandleConfigurationFileAsync_WithValidYmlExtension_ReturnsSuccess()
	{
		// Arrange
		var testRepoPath = Path.Combine(_testDirectory, "another-repo");
		var outputPath = Path.Combine(_testDirectory, "output");
		Directory.CreateDirectory(testRepoPath);

		var configPath = Path.Combine(_testDirectory, "config.yml");
		var yamlConfig = $"""
		global:
		  format: markdown
		  output-dir: {outputPath}
		repositories:
		  - name: AnotherRepo
		    path: {testRepoPath}
		    commands:
		      - health
		""";
		File.WriteAllText(configPath, yamlConfig);

		var pluginLoader = new PluginLoader();
		await pluginLoader.LoadInternalPluginsAsync();
		var handler = new ConfigurationCommandHandler(pluginLoader);

		// Act
		var result = await handler.HandleConfigurationFileAsync(configPath);

		// Assert
		Assert.Equal(0, result);
		Assert.True(Directory.Exists(outputPath));
		Assert.True(File.Exists(Path.Combine(outputPath, "AnotherRepo_health.md")));
	}

	[Fact]
	public async Task HandleConfigurationFileAsync_WithMissingFile_ReturnsFailure()
	{
		// Arrange
		var configPath = Path.Combine(_testDirectory, "nonexistent.json");
		var pluginLoader = new PluginLoader();
		await pluginLoader.LoadInternalPluginsAsync();
		var handler = new ConfigurationCommandHandler(pluginLoader);

		// Act
		var result = await handler.HandleConfigurationFileAsync(configPath);

		// Assert
		Assert.Equal(1, result);
	}

	[Fact]
	public async Task HandleConfigurationFileAsync_WithInvalidJsonFormat_ReturnsFailure()
	{
		// Arrange
		var configPath = Path.Combine(_testDirectory, "invalid.json");
		var invalidJson = "{ this is not valid json }";
		File.WriteAllText(configPath, invalidJson);

		var pluginLoader = new PluginLoader();
		await pluginLoader.LoadInternalPluginsAsync();
		var handler = new ConfigurationCommandHandler(pluginLoader);

		// Act
		var result = await handler.HandleConfigurationFileAsync(configPath);

		// Assert
		Assert.Equal(1, result);
	}

	[Fact]
	public async Task HandleConfigurationFileAsync_WithInvalidYamlFormat_ReturnsFailure()
	{
		// Arrange
		var configPath = Path.Combine(_testDirectory, "invalid.yaml");
		var invalidYaml = """
		global:
		  - this
		  - is
		  not: [valid, yaml structure
		""";
		File.WriteAllText(configPath, invalidYaml);

		var pluginLoader = new PluginLoader();
		await pluginLoader.LoadInternalPluginsAsync();
		var handler = new ConfigurationCommandHandler(pluginLoader);

		// Act
		var result = await handler.HandleConfigurationFileAsync(configPath);

		// Assert
		Assert.Equal(1, result);
	}

	[Fact]
	public async Task HandleConfigurationFileAsync_WithUnsupportedFileExtension_ReturnsFailure()
	{
		// Arrange
		var configPath = Path.Combine(_testDirectory, "config.txt");
		var content = "some config content";
		File.WriteAllText(configPath, content);

		var pluginLoader = new PluginLoader();
		await pluginLoader.LoadInternalPluginsAsync();
		var handler = new ConfigurationCommandHandler(pluginLoader);

		// Act
		var result = await handler.HandleConfigurationFileAsync(configPath);

		// Assert
		Assert.Equal(1, result);
	}

	[Fact]
	public async Task HandleConfigurationFileAsync_WithMultipleRepositories_ProcessesAll()
	{
		// Arrange
		var repo1Path = Path.Combine(_testDirectory, "repo1");
		var repo2Path = Path.Combine(_testDirectory, "repo2");
		var repo3Path = Path.Combine(_testDirectory, "repo3");
		var outputPath = Path.Combine(_testDirectory, "output");
		Directory.CreateDirectory(repo1Path);
		Directory.CreateDirectory(repo2Path);
		Directory.CreateDirectory(repo3Path);

		var configPath = Path.Combine(_testDirectory, "multi-repo.json");
		var jsonConfig = $$"""
		{
			"global": {
				"format": "markdown",
				"output-dir": "{{outputPath.Replace("\\", "\\\\")}}"
			},
			"repositories": [
				{
					"name": "Repo1",
					"path": "{{repo1Path.Replace("\\", "\\\\")}}",
					"commands": ["health"]
				},
				{
					"name": "Repo2",
					"path": "{{repo2Path.Replace("\\", "\\\\")}}",
					"commands": ["bom"]
				},
				{
					"name": "Repo3",
					"path": "{{repo3Path.Replace("\\", "\\\\")}}",
					"commands": ["health", "bom"]
				}
			]
		}
		""";
		File.WriteAllText(configPath, jsonConfig);

		var pluginLoader = new PluginLoader();
		await pluginLoader.LoadInternalPluginsAsync();
		var handler = new ConfigurationCommandHandler(pluginLoader);

		// Act
		var result = await handler.HandleConfigurationFileAsync(configPath);

		// Assert
		Assert.Equal(0, result);
		Assert.True(File.Exists(Path.Combine(outputPath, "Repo1_health.md")));
		Assert.True(File.Exists(Path.Combine(outputPath, "Repo2_bom.md")));
		Assert.True(File.Exists(Path.Combine(outputPath, "Repo3_health.md")));
		Assert.True(File.Exists(Path.Combine(outputPath, "Repo3_bom.md")));
	}

	[Fact]
	public async Task HandleConfigurationFileAsync_WithEmptyRepositories_ReturnsSuccess()
	{
		// Arrange
		var configPath = Path.Combine(_testDirectory, "empty-repos.json");
		var jsonConfig = """
		{
			"global": {
				"format": "markdown",
				"output-dir": "./output"
			},
			"repositories": []
		}
		""";
		File.WriteAllText(configPath, jsonConfig);

		var pluginLoader = new PluginLoader();
		await pluginLoader.LoadInternalPluginsAsync();
		var handler = new ConfigurationCommandHandler(pluginLoader);

		// Act
		var result = await handler.HandleConfigurationFileAsync(configPath);

		// Assert
		Assert.Equal(0, result);
	}

	[Fact]
	public async Task HandleConfigurationFileAsync_WithUnknownCommand_ContinuesProcessing()
	{
		// Arrange
		var testRepoPath = Path.Combine(_testDirectory, "test-repo");
		var outputPath = Path.Combine(_testDirectory, "output");
		Directory.CreateDirectory(testRepoPath);

		var configPath = Path.Combine(_testDirectory, "unknown-command.json");
		var jsonConfig = $$"""
		{
			"global": {
				"format": "markdown",
				"output-dir": "{{outputPath.Replace("\\", "\\\\")}}"
			},
			"repositories": [
				{
					"name": "TestRepo",
					"path": "{{testRepoPath.Replace("\\", "\\\\")}}",
					"commands": ["unknown-command", "health"]
				}
			]
		}
		""";
		File.WriteAllText(configPath, jsonConfig);

		var pluginLoader = new PluginLoader();
		await pluginLoader.LoadInternalPluginsAsync();
		var handler = new ConfigurationCommandHandler(pluginLoader);

		// Act
		var result = await handler.HandleConfigurationFileAsync(configPath);

		// Assert - Should complete successfully even with unknown command, but health should succeed
		Assert.Equal(0, result);
		Assert.True(File.Exists(Path.Combine(outputPath, "TestRepo_health.md")));
	}

	[Fact]
	public async Task HandleConfigurationFileAsync_WithComplexYamlStructure_ParsesCorrectly()
	{
		// Arrange
		var mainPath = Path.Combine(_testDirectory, "src", "main");
		var testPath = Path.Combine(_testDirectory, "src", "test");
		var docsPath = Path.Combine(_testDirectory, "docs");
		var outputPath = Path.Combine(_testDirectory, "reports", "output");
		Directory.CreateDirectory(mainPath);
		Directory.CreateDirectory(testPath);
		Directory.CreateDirectory(docsPath);

		var configPath = Path.Combine(_testDirectory, "complex.yaml");
		var yamlConfig = $"""
		global:
		  format: markdown
		  output-dir: {outputPath}

		repositories:
		  - name: MainProject
		    path: {mainPath}
		    commands:
		      - health
		      - bom
		      - vulnerabilities

		  - name: TestProject
		    path: {testPath}
		    commands:
		      - health

		  - name: DocsProject
		    path: {docsPath}
		    commands:
		      - bom
		""";
		File.WriteAllText(configPath, yamlConfig);

		var pluginLoader = new PluginLoader();
		await pluginLoader.LoadInternalPluginsAsync();
		var handler = new ConfigurationCommandHandler(pluginLoader);

		// Act
		var result = await handler.HandleConfigurationFileAsync(configPath);

		// Assert
		Assert.Equal(0, result);
		Assert.True(File.Exists(Path.Combine(outputPath, "MainProject_health.md")));
		Assert.True(File.Exists(Path.Combine(outputPath, "MainProject_bom.md")));
		Assert.True(File.Exists(Path.Combine(outputPath, "MainProject_vulnerabilities.md")));
		Assert.True(File.Exists(Path.Combine(outputPath, "TestProject_health.md")));
		Assert.True(File.Exists(Path.Combine(outputPath, "DocsProject_bom.md")));
	}

	[Fact]
	public async Task HandleConfigurationFileAsync_OutputFilesContainValidMarkdown()
	{
		// Arrange
		var testRepoPath = Path.Combine(_testDirectory, "test-repo");
		var outputPath = Path.Combine(_testDirectory, "output");
		Directory.CreateDirectory(testRepoPath);

		var configPath = Path.Combine(_testDirectory, "config.json");
		var jsonConfig = $$"""
		{
			"global": {
				"format": "markdown",
				"output-dir": "{{outputPath.Replace("\\", "\\\\")}}"
			},
			"repositories": [
				{
					"name": "TestRepo",
					"path": "{{testRepoPath.Replace("\\", "\\\\")}}",
					"commands": ["health"]
				}
			]
		}
		""";
		File.WriteAllText(configPath, jsonConfig);

		var pluginLoader = new PluginLoader();
		await pluginLoader.LoadInternalPluginsAsync();
		var handler = new ConfigurationCommandHandler(pluginLoader);

		// Act
		var result = await handler.HandleConfigurationFileAsync(configPath);

		// Assert
		Assert.Equal(0, result);

		var healthReportPath = Path.Combine(outputPath, "TestRepo_health.md");
		Assert.True(File.Exists(healthReportPath));

		var content = File.ReadAllText(healthReportPath);
		Assert.Contains("# CodeMedic", content);
		Assert.Contains("Repository Health Dashboard", content);
		Assert.Contains(".NET Repository Health Analysis Tool", content);

		// Verify it's valid markdown with proper headers
		Assert.Contains("##", content); // Should have at least one section header
	}
}
