using System.Runtime.InteropServices;
using InventorAddinManager.Loading;

namespace InventorAddinManager.Automation;

/// <summary>
/// 	The object that <c>ApplicationAddIn.Automation</c> gives for the manager.
/// 	A script, an iLogic rule, or a test in another process uses it to run a command with no window.
/// </summary>
/// <remarks>
/// 	COM calls it through <c>IDispatch</c>, so a caller uses late binding, ex. <c>dynamic</c> in C#.
/// 	A run here does not change <b>Run Last</b>, and does not show the result window.
/// 	The result window is modal, so it would stop a caller until a user closes it.
/// </remarks>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
public sealed class ManagerAutomation
{
	private readonly Func<CommandReference, bool, CommandRunResult> _runCommand;

	/// <param name="runCommand">
	/// 	Runs a command and writes it to the manager's log.
	/// </param>
	internal ManagerAutomation(Func<CommandReference, bool, CommandRunResult> runCommand)
	{
		_runCommand = runCommand;
	}

	/// <summary>
	/// 	Runs a command from the newest build.
	/// </summary>
	/// <param name="assemblyPath">
	/// 	Path of the assembly in the build output.
	/// </param>
	/// <param name="typeName">
	/// 	Full name of the type that has the <c>Execute</c> method.
	/// </param>
	/// <param name="wrapInTransaction">
	/// 	<see langword="true" /> to put all changes to the active document in one undo step.
	/// </param>
	/// <returns>
	/// 	The result of the run.
	/// </returns>
	public ManagerRunResult Run(string assemblyPath, string typeName, bool wrapInTransaction)
	{
		return new ManagerRunResult(_runCommand(new CommandReference(assemblyPath, typeName), wrapInTransaction));
	}

	/// <summary>
	/// 	Lists the commands in a build output.
	/// </summary>
	/// <param name="assemblyPath">
	/// 	Path of the assembly in the build output.
	/// </param>
	/// <returns>
	/// 	The full type name of each command, sorted.
	/// </returns>
	public string[] ListCommands(string assemblyPath)
	{
		return [.. CommandCatalog.Discover(assemblyPath).Select(static command => command.TypeName)];
	}
}
