using System.Reflection;

namespace InventorAddinManager.Tests;

/// <summary>
/// 	The build output of InventorAddinManager.Tests.Commands, and a temporary shadow root for each test class.
/// </summary>
internal static class TestCommands
{
	public const string Namespace = "InventorAddinManager.Tests.Commands";

	/// <summary>
	/// 	Gets the path of the command assembly in its build output.
	/// </summary>
	public static string AssemblyPath { get; } = typeof(TestCommands).Assembly
		.GetCustomAttributes<AssemblyMetadataAttribute>()
		.Single(static attribute => attribute.Key == "CommandsAssemblyPath")
		.Value!;

	/// <summary>
	/// 	Gets the full name of a type in the command assembly.
	/// </summary>
	public static string TypeName(string shortName)
	{
		return $"{Namespace}.{shortName}";
	}
}

/// <summary>
/// 	A temporary folder that the test deletes at the end, best effort.
/// </summary>
/// <remarks>
/// 	A build that did not unload keeps its files open, so a delete can fail. The system cleans the temporary folder later.
/// </remarks>
internal sealed class TemporaryFolder : IDisposable
{
	public TemporaryFolder()
	{
		Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "InventorAddinManager.Tests", Guid.NewGuid().ToString("N"));
		_ = Directory.CreateDirectory(Path);
	}

	public string Path { get; }

	public void Dispose()
	{
		try
		{
			Directory.Delete(Path, recursive: true);
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
		}
	}
}
