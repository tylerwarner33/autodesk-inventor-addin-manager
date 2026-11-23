using System.Diagnostics;
using System.Globalization;
using System.Runtime.Loader;
using Newtonsoft.Json;

namespace InventorAddinManager.Tests.Commands;

/// <summary>
/// 	A static command that returns a fixed text.
/// </summary>
public static class StaticCommand
{
	public const string ReturnText = "static";

	public static string Execute()
	{
		return ReturnText;
	}
}

/// <summary>
/// 	An instance command whose constructor gets the Inventor session.
/// </summary>
/// <param name="inventorApplication">
/// 	The session, or <see langword="null" /> in a unit test.
/// </param>
public sealed class SessionCommand(Inventor.Application? inventorApplication)
{
	public const string NoSessionText = "no session";

	public string Execute()
	{
		return inventorApplication?.SoftwareVersion.DisplayVersion ?? NoSessionText;
	}
}

/// <summary>
/// 	A command whose only parameter has a default value.
/// </summary>
public sealed class DefaultValueCommand
{
	public const int DefaultCount = 3;

	public int Execute(int count = DefaultCount)
	{
		return count;
	}
}

/// <summary>
/// 	A command that fails.
/// </summary>
public static class FailingCommand
{
	public const string Message = "The fixture command failed.";

	public static void Execute()
	{
		throw new InvalidOperationException(Message);
	}
}

/// <summary>
/// 	A command that counts its runs in a static field.
/// </summary>
/// <remarks>
/// 	Each run is a fresh load, so each run returns 1.
/// </remarks>
public static class RunCounterCommand
{
	private static int _runs;

	public static int Execute()
	{
		return ++_runs;
	}
}

/// <summary>
/// 	A command that leaves a reference to its own type in the default context, so its build cannot unload.
/// </summary>
public sealed class LeakingCommand
{
	public const string DataName = "InventorAddinManager.Tests.LeakingCommand";

	public void Execute()
	{
		AppDomain.CurrentDomain.SetData(DataName, this);
	}
}

/// <summary>
/// 	Returns the file version and the path of the Newtonsoft.Json that the command uses, separated by '|'.
/// </summary>
public static class NewtonsoftVersionCommand
{
	public static string Execute()
	{
		string location = typeof(JsonConvert).Assembly.Location;

		return string.Create(CultureInfo.InvariantCulture, $"{FileVersionInfo.GetVersionInfo(location).FileVersion}|{location}");
	}
}

/// <summary>
/// 	Returns the path that <see cref="System.Reflection.Assembly.Location" /> gives for the command assembly.
/// </summary>
public static class LocationCommand
{
	public static string Execute()
	{
		return typeof(LocationCommand).Assembly.Location;
	}
}

/// <summary>
/// 	Returns the name of the load context that holds the Inventor interop the command uses.
/// </summary>
public static class InteropContextCommand
{
	public static string? Execute()
	{
		return AssemblyLoadContext.GetLoadContext(typeof(Inventor.Application).Assembly)?.Name;
	}
}

/// <summary>
/// 	Not a command: it has no <c>Execute</c> method.
/// </summary>
public sealed class NoEntryMethod
{
	public void Run()
	{
	}
}

/// <summary>
/// 	Not a command: the manager cannot fill the parameter.
/// </summary>
public sealed class RequiredParameterCommand
{
	public string Execute(string text)
	{
		return text;
	}
}

/// <summary>
/// 	Not a command: the manager cannot create an abstract type.
/// </summary>
public abstract class AbstractCommand
{
	public void Execute()
	{
	}
}

/// <summary>
/// 	Not a command: the type is generic.
/// </summary>
public sealed class GenericCommand<T>
{
	public T? Execute()
	{
		return default;
	}
}

/// <summary>
/// 	Not a command: the type is not public.
/// </summary>
internal sealed class InternalCommand
{
	public void Execute()
	{
	}
}

/// <summary>
/// 	Not a command: it has no public constructor.
/// </summary>
public sealed class NoPublicConstructorCommand
{
	private NoPublicConstructorCommand()
	{
	}

	public void Execute()
	{
	}
}
