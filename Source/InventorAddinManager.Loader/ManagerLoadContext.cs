using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace InventorAddinManager.Loader;

/// <summary>
/// 	The load context of the manager on Inventor releases that cannot isolate an add-in themselves.
/// </summary>
/// <remarks>
/// 	The manager and its packages load here, and only here.
/// 	<see cref="Load" /> returns null for each assembly that the manager's deps.json does not name.
/// 	The interop then comes from the default context, so the COM types keep one managed identity.
/// 	The context is not collectible: the manager stays loaded until Inventor closes, as an add-in in the default context does.
/// </remarks>
internal sealed class ManagerLoadContext : AssemblyLoadContext
{
	/// <summary>
	/// 	The subfolder that holds the full build output of the manager.
	/// </summary>
	public const string ManagerFolderName = "App";

	/// <summary>
	/// 	The simple name of the manager assembly in <see cref="ManagerFolderName" />.
	/// </summary>
	public const string ManagerAssemblyName = "InventorAddinManager";

	private static ManagerLoadContext? _instance;

	private readonly AssemblyDependencyResolver _resolver;
	private readonly Assembly _managerAssembly;

	private ManagerLoadContext(string managerAssemblyPath) : base(ManagerAssemblyName)
	{
		_resolver = new AssemblyDependencyResolver(managerAssemblyPath);
		_managerAssembly = LoadFromAssemblyPath(managerAssemblyPath);
	}

	/// <summary>
	/// 	Creates an instance of a manager type in the manager's load context.
	/// </summary>
	/// <param name="typeFullName">
	/// 	The name of the type with its namespace, ex. "InventorAddinManager.AddInServer".
	/// </param>
	/// <returns>
	/// 	The new instance.
	/// </returns>
	public static object CreateInstance(string typeFullName)
	{
		_instance ??= Create();

		return _instance._managerAssembly.CreateInstance(typeFullName)
			?? throw new TypeLoadException($"Type '{typeFullName}' was not found in '{_instance._managerAssembly.Location}'.");
	}

	/// <summary>
	/// 	Resolves a managed dependency from the manager's deps.json.
	/// </summary>
	/// <param name="assemblyName">
	/// 	The assembly to resolve.
	/// </param>
	/// <returns>
	/// 	The loaded assembly, or null to use the default context.
	/// </returns>
	protected override Assembly? Load(AssemblyName assemblyName)
	{
		string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

		return assemblyPath is not null
			? LoadFromAssemblyPath(assemblyPath)
			: null;
	}

	/// <summary>
	/// 	Resolves a native dependency from the manager's deps.json.
	/// </summary>
	/// <param name="unmanagedDllName">
	/// 	The native library to resolve.
	/// </param>
	/// <returns>
	/// 	A handle to the loaded library, or <see cref="nint.Zero" /> to use the default context.
	/// </returns>
	protected override nint LoadUnmanagedDll(string unmanagedDllName)
	{
		string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

		return libraryPath is not null
			? LoadUnmanagedDllFromPath(libraryPath)
			: nint.Zero;
	}

	private static ManagerLoadContext Create()
	{
		string loaderDirectory = Path.GetDirectoryName(typeof(ManagerLoadContext).Assembly.Location)!;
		string managerAssemblyPath = Path.Combine(loaderDirectory, ManagerFolderName, $"{ManagerAssemblyName}.dll");

		if (File.Exists(managerAssemblyPath) is false)
		{
			throw new FileNotFoundException(
				$"The manager was not found at '{managerAssemblyPath}'. The '{ManagerFolderName}' subfolder must hold the full build output of the manager.",
				managerAssemblyPath);
		}

		return new ManagerLoadContext(managerAssemblyPath);
	}
}
