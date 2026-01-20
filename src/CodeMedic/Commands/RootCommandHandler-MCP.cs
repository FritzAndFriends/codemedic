using System.Text.Json;
using System.Text.Json.Nodes;
using CodeMedic.Abstractions.Plugins;
using CodeMedic.Output;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeMedic.Commands;

public partial class RootCommandHandler
{

	private static async Task ConfigureMcpServer(string version)
	{

		var options = new McpServerOptions
		{
			ServerInfo = new Implementation
			{
				Name = "CodeMedic",
				Version = version,
				Description = "Project analysis and code health assessment tool.",
			},
			Handlers = new McpServerHandlers
			{
				ListToolsHandler = ListTools,
				CallToolHandler = CallTool
			}
		};

		await using McpServer server = McpServer.Create(new StdioServerTransport("CodeMedic"), options);
		await server.RunAsync();

	}

	private static async ValueTask<CallToolResult> CallTool(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
	{

		if (request.Params is null)
		{
			throw new InvalidOperationException("No tool specified in the request.");
		}

		if (!_pluginLoader.Commands.TryGetValue(request.Params.Name, out var command))
		{
			throw new InvalidOperationException($"Tool '{request.Params.Name}' not found.");
		}

		// Convert input parameters to command-line arguments
		var argsList = new List<string>();

		try {
		if (command.Arguments is not null && request.Params.Arguments is not null)
		{
			foreach (var arg in command.Arguments)
			{
				string? value = null;
				if (arg.LongName is not null && request.Params.Arguments.TryGetValue(arg.LongName, out var longProp))
				{
					value = longProp.GetString();
				}
				else if (arg.ShortName is not null && request.Params.Arguments.TryGetValue(arg.ShortName, out var shortProp))
				{
					value = shortProp.GetString();
				}

				if (value is not null)
				{
					if (arg.LongName is not null)
					{
						argsList.Add($"--{arg.LongName}");
					}
					else if (arg.ShortName is not null)
					{
						argsList.Add($"-{arg.ShortName}");
					}
					argsList.Add(value);
				}
			}
		}
		}
		catch (Exception ex)
		{

			System.Console.WriteLine(ex.ToString());

			return new CallToolResult
			{
				IsError = true,
				Content = [new TextContentBlock { Text = $"Error processing arguments for tool '{command.Name}': {ex.Message}" }]
			};
		}

		var args = argsList.ToArray();

		var sb = new StringWriter();
		var renderer = new McpCommandRenderer(sb);


		var exitCode = 0;

		try {
			// create the handler payload
			var payload = new HandlerPayload
			{
				Args = args,
				Renderer = renderer,
				ProjectTitle = $"CodeMedic Tool Execution - {command.Name}"
			};
			exitCode = await command.Handler(payload);
		}
		catch (Exception ex)
		{

			System.Console.WriteLine(ex.ToString());

			exitCode = 1;
			return new CallToolResult
			{
				IsError = true,
				Content = [new TextContentBlock { Text = $"Error executing tool '{command.Name}': {ex.Message}" }]
			};
		}

		// get the JSON from the StringWriter and return as StructuredContent
		var outputJson = sb.ToString().Replace("\n", "\\n").Replace("\r", "\\r").Replace("\"", "\\\"");
		return new CallToolResult
		{
			IsError = false,
			StructuredContent = JsonSerializer.Deserialize<JsonNode>($$"""
				{
					"exitCode": {{exitCode}},
					"output": "{{outputJson}}"
				}
				""")
		};

	}

	private static async ValueTask<ListToolsResult> ListTools(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken)
	{

		var result = new ListToolsResult
		{
			Tools = new List<Tool>()
		};

		foreach (var command in _pluginLoader.Commands.Values)
		{

			var sbArguments = new System.Text.StringBuilder();
			var requiredArguments = new List<string>();
			if (command.Arguments is not null)
			{
				foreach (var arg in command.Arguments)
				{

					if (sbArguments.Length > 0)
					{
						sbArguments.Append(", ");
					}

					// generate argument representation as a JSON object
					sbArguments.Append($$"""
						"{{arg.LongName ?? arg.ShortName}}": {
							"type": "string",
							"description": "{{arg.Description}}"
						}
						""");
					if (arg.IsRequired)
					{
						requiredArguments.Add(arg.LongName ?? arg.ShortName!);
					}
				}
			}

			var requiredArray = requiredArguments.Count > 0
				? string.Join(", ", requiredArguments.Select(a => $"\"{a}\""))
				: "";

			var tool = new Tool
			{
				Name = command.Name,
				Description = command.Description,
				InputSchema = JsonSerializer.Deserialize<JsonElement>($$"""
					{
						"type": "object",
						"properties": {
							{{sbArguments.ToString()}}
						},
						"required": [{{requiredArray}}]
					}
					"""),
			};

			result.Tools.Add(tool);
		}

		return await ValueTask.FromResult(result);


	}
}
