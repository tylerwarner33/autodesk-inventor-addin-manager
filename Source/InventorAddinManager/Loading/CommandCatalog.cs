using System.IO;
using System.Reflection;

namespace InventorAddinManager.Loading;

/// <summary>
/// 	Lists the commands in a build output.
/// </summary>
internal static class CommandCatalog
{
	/// <summary>
	/// 	Loads a shadow copy of the assembly, lists its commands, and unloads it again.
	/// </summary>
	/// <param name="assemblyPath">
	/// 	Path of the assembly in the build output.
	/// </param>
	/// <returns>
	/// 	The commands, sorted by type name.
	/// </returns>
	public static IReadOnlyList<CommandReference> Discover(string assemblyPath)
	{
		if (File.Exists(assemblyPath) is false)
		{
			throw new FileNotFoundException($"'{assemblyPath}' does not exist. Build the project first.", assemblyPath);
		}

		string shadowAssemblyPath = ShadowCopy.Create(assemblyPath);
		CommandLoadContext context = new(shadowAssemblyPath);
		try
		{
			Assembly assembly = context.LoadFromAssemblyPath(shadowAssemblyPath);

			// Only strings leave this method, so nothing keeps the context alive after Unload.
			List<CommandReference> commands = [];
			foreach (Type type in GetLoadableTypes(assembly))
			{
				if (IsCommandSafe(type))
				{
					commands.Add(new CommandReference(assemblyPath, type.FullName!));
				}
			}

			return [.. commands.OrderBy(static command => command.TypeName, StringComparer.OrdinalIgnoreCase)];
		}
		finally
		{
			context.Unload();
		}
	}

	/// <summary>
	/// 	Gets the types that load, and skips the types with a missing dependency.
	/// </summary>
	private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException exception)
		{
			return exception.Types.OfType<Type>();
		}
	}

	/// <summary>
	/// 	Tells if the type is a command, and treats a type whose members do not load as not a command.
	/// </summary>
	private static bool IsCommandSafe(Type type)
	{
		try
		{
			return CommandBinder.IsCommand(type);
		}
		catch (Exception exception) when (exception is TypeLoadException or FileNotFoundException or FileLoadException)
		{
			return false;
		}
	}
}
