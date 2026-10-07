using System.Globalization;
using System.Runtime.Loader;
using System.Text;
using Inventor;

namespace SampleCommands;

/// <summary>
/// 	Reports the active document, and where the build that runs was loaded from.
/// </summary>
/// <remarks>
/// 	The manager shows the returned text.
/// 	Change the text, build, and click <b>Run Last</b> to see the new build run with Inventor still open.
/// </remarks>
public sealed class ActiveDocumentReport
{
	public string Execute(Inventor.Application inventorApplication)
	{
		StringBuilder report = new();

		if (inventorApplication.ActiveDocument is Document document)
		{
			_ = report.AppendLine(CultureInfo.InvariantCulture, $"Document:    {document.DisplayName}");
			_ = report.AppendLine(CultureInfo.InvariantCulture, $"Type:        {document.DocumentType}");
			_ = report.AppendLine(CultureInfo.InvariantCulture, $"File:        {document.FullFileName}");
			_ = report.AppendLine(CultureInfo.InvariantCulture, $"References:  {document.ReferencedDocuments.Count}");
			_ = report.AppendLine(CultureInfo.InvariantCulture, $"Parameters:  {CountParameters(document)}");
		}
		else
		{
			_ = report.AppendLine("No document is open.");
		}

		// Each run gives a new shadow folder and a new load context.
		_ = report.AppendLine();
		_ = report.AppendLine(CultureInfo.InvariantCulture, $"Loaded from: {typeof(ActiveDocumentReport).Assembly.Location}");
		_ = report.AppendLine(CultureInfo.InvariantCulture, $"Context:     {AssemblyLoadContext.GetLoadContext(typeof(ActiveDocumentReport).Assembly)?.Name}");

		return report.ToString();
	}

	private static string CountParameters(Document document)
	{
		return document switch
		{
			PartDocument part => part.ComponentDefinition.Parameters.Count.ToString(CultureInfo.InvariantCulture),
			AssemblyDocument assembly => assembly.ComponentDefinition.Parameters.Count.ToString(CultureInfo.InvariantCulture),
			_ => "not applicable",
		};
	}
}
