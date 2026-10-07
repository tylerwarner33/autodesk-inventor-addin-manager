using System.Text.Json.Serialization;

namespace InventorAddinManager.Loading;

/// <summary>
/// 	A command that the manager can run: one public type in one build output.
/// </summary>
/// <param name="AssemblyPath">
/// 	Path of the assembly in the build output, not in a shadow copy.
/// </param>
/// <param name="TypeName">
/// 	Full name of the type that has the <c>Execute</c> method.
/// </param>
public sealed record CommandReference(string AssemblyPath, string TypeName)
{
	/// <summary>
	/// 	Gets the type name without its namespace.
	/// </summary>
	[JsonIgnore]
	public string ShortName => TypeName[(TypeName.LastIndexOf('.') + 1)..];
}
