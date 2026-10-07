using System.IO;

namespace InventorAddinManager.Views;

/// <summary>
/// 	One build output in the manager list.
/// </summary>
/// <remarks>
/// 	Public, because WPF data binding reads the properties.
/// </remarks>
/// <param name="AssemblyPath">
/// 	Path of the assembly in the build output.
/// </param>
public sealed record BuildOutputItem(string AssemblyPath)
{
	/// <summary>
	/// 	Gets the file name of the assembly.
	/// </summary>
	public string FileName => Path.GetFileName(AssemblyPath);

	/// <summary>
	/// 	Gets the folder that holds the assembly.
	/// </summary>
	public string FolderPath => Path.GetDirectoryName(AssemblyPath) ?? string.Empty;
}
