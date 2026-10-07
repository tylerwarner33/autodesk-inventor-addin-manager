using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace InventorAddinManager.Loading;

/// <summary>
/// 	A collectible load context for one shadow copy of a command build.
/// </summary>
/// <remarks>
/// 	Dependencies resolve from the shadow copy first, so a different version in the default context never wins.
/// 	The Inventor interop is the exception.
/// 	It always resolves to the manager's own copy, so <c>Inventor.Application</c> is one type for both sides.
/// 	A build that ships its own copy of the interop by mistake still gets Inventor's copy.
/// </remarks>
/// <param name="mainAssemblyPath">
/// 	Path of the command assembly in the shadow copy.
/// 	Its folder and its <c>.deps.json</c> file control how dependencies resolve.
/// </param>
internal sealed class CommandLoadContext(string mainAssemblyPath)
	: AssemblyLoadContext($"{nameof(InventorAddinManager)}:{Path.GetFileNameWithoutExtension(mainAssemblyPath)}", isCollectible: true)
{
	private static readonly Assembly _inventorInteropAssembly = typeof(Inventor.Application).Assembly;
	private static readonly string? _inventorInteropName = _inventorInteropAssembly.GetName().Name;

	private readonly AssemblyDependencyResolver _resolver = new(mainAssemblyPath);

	protected override Assembly? Load(AssemblyName assemblyName)
	{
		if (string.Equals(assemblyName.Name, _inventorInteropName, StringComparison.OrdinalIgnoreCase))
		{
			return _inventorInteropAssembly;
		}

		string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

		// Null falls back to the default context, which holds the framework assemblies.
		return assemblyPath is null ? null : LoadFromAssemblyPath(assemblyPath);
	}

	protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
	{
		string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

		return libraryPath is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(libraryPath);
	}
}
