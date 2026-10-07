using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Newtonsoft.Json;

namespace SampleCommands;

/// <summary>
/// 	Reports each loaded copy of Newtonsoft.Json, and the copy that this command uses.
/// </summary>
/// <remarks>
/// 	Inventor ships its own Newtonsoft.Json.
/// 	The command must get the version from its own build output, not Inventor's version.
/// </remarks>
public static class PackageVersionReport
{
	public static string Execute()
	{
		StringBuilder report = new();

		Assembly usedAssembly = typeof(JsonConvert).Assembly;
		_ = report.AppendLine("This command uses:");
		AppendAssembly(report, usedAssembly);

		// The serializer must work too, not only load.
		_ = report.AppendLine(CultureInfo.InvariantCulture, $"     Serialize: {JsonConvert.SerializeObject(new { Check = "ok" })}");
		_ = report.AppendLine();

		_ = report.AppendLine("Every loaded Newtonsoft.Json:");
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(static assembly => assembly.GetName().Name == "Newtonsoft.Json"))
		{
			AppendAssembly(report, assembly);
		}

		return report.ToString();
	}

	private static void AppendAssembly(StringBuilder report, Assembly assembly)
	{
		string fileVersion = string.IsNullOrEmpty(assembly.Location) ? "n/a" : FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion ?? "n/a";

		_ = report.AppendLine(CultureInfo.InvariantCulture, $"   - File version {fileVersion}, context '{AssemblyLoadContext.GetLoadContext(assembly)?.Name}'");
		_ = report.AppendLine(CultureInfo.InvariantCulture, $"     {assembly.Location}");
	}
}
