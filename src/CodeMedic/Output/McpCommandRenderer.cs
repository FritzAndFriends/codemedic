using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeMedic.Output;

/// <summary>
/// MCP command renderer that outputs to a provided TextWriter.
/// </summary>
public class McpCommandRenderer : IRenderer
{
	private readonly TextWriter _TextWriter;

	/// <summary>
	/// Build a new McpCommandRenderer.
	/// </summary>
	/// <param name="textWriter">The TextWriter to output to.</param>
	public McpCommandRenderer(TextWriter textWriter)
	{
		_TextWriter = textWriter;
	}

	/// <summary>
	/// Renders a banner as JSON output.
	/// </summary>
	/// <param name="subtitle">Optional subtitle text.</param>
	public void RenderBanner(string subtitle = "")
	{
		var bannerData = new
		{
			type = "banner",
			title = "CodeMedic",
			subtitle = subtitle
		};
		_TextWriter.WriteLine(JsonSerializer.Serialize(bannerData));
	}

	/// <summary>
	/// Renders an error message as JSON output.
	/// </summary>
	/// <param name="message">The error message to render.</param>
	public void RenderError(string message)
	{
		var errorData = new
		{
			type = "error",
			message = message
		};
		_TextWriter.WriteLine(JsonSerializer.Serialize(errorData));
	}

	/// <summary>
	/// Renders a footer as JSON output.
	/// </summary>
	/// <param name="footer">The footer content to render.</param>
	public void RenderFooter(string footer)
	{
		var footerData = new
		{
			type = "footer",
			content = footer
		};
		_TextWriter.WriteLine(JsonSerializer.Serialize(footerData));
	}

	/// <summary>
	/// Renders an informational message as JSON output.
	/// </summary>
	/// <param name="message">The info message to render.</param>
	public void RenderInfo(string message)
	{
		var infoData = new
		{
			type = "info",
			message = message
		};
		_TextWriter.WriteLine(JsonSerializer.Serialize(infoData));
	}

	/// <summary>
	/// Renders a report object as JSON output.
	/// </summary>
	/// <param name="report">The report object to serialize and render.</param>
	public void RenderReport(object report)
	{
		var options = new JsonSerializerOptions
		{
			WriteIndented = false,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			Converters = { new PolymorphicReportElementConverter() }
		};

		var reportData = new
		{
			type = "report",
			data = report
		};
		_TextWriter.WriteLine(JsonSerializer.Serialize(reportData, options));
	}

	/// <summary>
	/// Custom JSON converter that handles polymorphic IReportElement serialization by including type information.
	/// </summary>
	private class PolymorphicReportElementConverter : JsonConverter<object>
	{
		public override bool CanConvert(Type typeToConvert)
		{
			return typeToConvert.GetInterfaces().Any(i => i.Name == "IReportElement") ||
			       typeToConvert.Name == "IReportElement";
		}

		public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			throw new NotImplementedException("Deserialization not supported");
		}

		public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
		{
			var actualType = value.GetType();

			writer.WriteStartObject();
			writer.WriteString("$type", actualType.Name);

			// Serialize all public properties of the concrete type
			foreach (var prop in actualType.GetProperties())
			{
				var propValue = prop.GetValue(value);
				if (propValue != null)
				{
					writer.WritePropertyName(JsonNamingPolicy.CamelCase.ConvertName(prop.Name));
					JsonSerializer.Serialize(writer, propValue, propValue.GetType(), options);
				}
			}

			writer.WriteEndObject();
		}
	}

	/// <summary>
	/// Renders a section header as JSON output.
	/// </summary>
	/// <param name="title">The section title to render.</param>
	public void RenderSectionHeader(string title)
	{
		var sectionData = new
		{
			type = "section_header",
			title = title
		};
		_TextWriter.WriteLine(JsonSerializer.Serialize(sectionData));
	}

	/// <summary>
	/// Renders a wait message while performing an asynchronous operation.
	/// </summary>
	/// <param name="message">The message to display while waiting.</param>
	/// <param name="operation"></param>
	/// <returns></returns>
	public Task RenderWaitAsync(string message, Func<Task> operation)
	{
		// don't wait
		return operation();
	}
}
