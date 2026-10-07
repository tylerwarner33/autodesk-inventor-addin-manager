using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace InventorAddinManager.Loading;

/// <summary>
/// 	Runs a command from a new shadow copy of its build output, then unloads that copy.
/// </summary>
/// <param name="inventorApplication">
/// 	The running Inventor session.
/// </param>
internal sealed class CommandRunner(Inventor.Application inventorApplication)
{
	/// <summary>
	/// 	The number of garbage collections to try before the manager reports that a build stays loaded.
	/// </summary>
	private const int _unloadAttempts = 3;

	/// <summary>
	/// 	Runs the command from the newest build.
	/// </summary>
	/// <param name="command">
	/// 	The command to run.
	/// </param>
	/// <param name="wrapInTransaction">
	/// 	<see langword="true" /> to put all changes to the active document in one undo step.
	/// </param>
	/// <returns>
	/// 	The result. It holds only strings, so it does not keep the build loaded.
	/// </returns>
	public CommandRunResult Run(CommandReference command, bool wrapInTransaction)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		(string? returnText, string? errorText, WeakReference contextReference) = Execute(command, wrapInTransaction);
		TimeSpan elapsed = stopwatch.Elapsed;

		bool isUnloaded = WaitForUnload(contextReference);

		return new CommandRunResult(command, errorText is null, returnText, errorText, elapsed, isUnloaded);
	}

	/// <summary>
	/// 	Loads the build, calls the command, and starts the unload.
	/// </summary>
	/// <remarks>
	/// 	No inlining: the local reference to the context must end with this method, or the context cannot be collected.
	/// 	Values from the command become strings here for the same reason.
	/// 	An object or an exception of a type from the build keeps the build loaded.
	/// </remarks>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private (string? ReturnText, string? ErrorText, WeakReference ContextReference) Execute(CommandReference command, bool wrapInTransaction)
	{
		if (File.Exists(command.AssemblyPath) is false)
		{
			return (null, $"'{command.AssemblyPath}' does not exist. Build the project first.", new WeakReference(null));
		}

		string shadowAssemblyPath = ShadowCopy.Create(command.AssemblyPath);
		CommandLoadContext context = new(shadowAssemblyPath);
		WeakReference contextReference = new(context, trackResurrection: true);
		Inventor.Transaction? transaction = null;

		try
		{
			Assembly assembly = context.LoadFromAssemblyPath(shadowAssemblyPath);
			Type type = assembly.GetType(command.TypeName, throwOnError: false)
				?? throw new InvalidOperationException($"The build has no public type '{command.TypeName}'. Open the manager to refresh the command list.");

			MethodInfo method = CommandBinder.FindEntryMethod(type)
				?? throw new InvalidOperationException($"'{command.TypeName}' has no public '{CommandBinder.EntryMethodName}' method with parameters the manager can fill.");

			object? instance = null;
			if (method.IsStatic is false)
			{
				ConstructorInfo constructor = CommandBinder.FindConstructor(type)
					?? throw new InvalidOperationException($"'{command.TypeName}' has no public constructor with parameters the manager can fill.");

				instance = constructor.Invoke(CommandBinder.BindArguments(constructor.GetParameters(), inventorApplication));
			}

			transaction = StartTransaction(command, wrapInTransaction);
			object? returnValue = method.Invoke(instance, CommandBinder.BindArguments(method.GetParameters(), inventorApplication));

			// Cleared before End, so a failure in End does not undo the work in the catch block.
			Inventor.Transaction? completedTransaction = transaction;
			transaction = null;
			completedTransaction?.End();

			return (returnValue?.ToString(), null, contextReference);
		}
		catch (Exception exception)
		{
			AbortTransaction(transaction);

			Exception reportedException = exception is TargetInvocationException { InnerException: Exception innerException }
				? innerException
				: exception;

			return (null, reportedException.ToString(), contextReference);
		}
		finally
		{
			context.Unload();
		}
	}

	/// <summary>
	/// 	Starts one undo step for the active document.
	/// </summary>
	/// <returns>
	/// 	The transaction, or <see langword="null" /> when the option is off or no document is open.
	/// </returns>
	private Inventor.Transaction? StartTransaction(CommandReference command, bool wrapInTransaction)
	{
		if (wrapInTransaction is false || inventorApplication.ActiveDocument is not Inventor._Document document)
		{
			return null;
		}

		return inventorApplication.TransactionManager.StartTransaction(document, $"Add-In Manager: {command.ShortName}");
	}

	private static void AbortTransaction(Inventor.Transaction? transaction)
	{
		try
		{
			transaction?.Abort();
		}
		catch (System.Runtime.InteropServices.COMException)
		{
			// The command can close the document, which ends the transaction too.
		}
	}

	/// <summary>
	/// 	Collects garbage until the context is gone, or the attempts run out.
	/// </summary>
	/// <returns>
	/// 	<see langword="true" /> when the build is unloaded.
	/// </returns>
	private static bool WaitForUnload(WeakReference contextReference)
	{
		for (int attempt = 0; contextReference.IsAlive && attempt < _unloadAttempts; attempt++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		return contextReference.IsAlive is false;
	}
}

/// <summary>
/// 	The result of one command run.
/// </summary>
/// <param name="Command">
/// 	The command that ran.
/// </param>
/// <param name="Succeeded">
/// 	<see langword="true" /> when the command returned without an exception.
/// </param>
/// <param name="ReturnText">
/// 	The text of the value that the command returned, or <see langword="null" />.
/// </param>
/// <param name="ErrorText">
/// 	The exception text, or <see langword="null" /> when the command succeeded.
/// </param>
/// <param name="Elapsed">
/// 	The time to copy, load, and run the command.
/// </param>
/// <param name="IsUnloaded">
/// 	<see langword="false" /> when something still holds a reference into the build after the run.
/// </param>
internal sealed record CommandRunResult(
	CommandReference Command,
	bool Succeeded,
	string? ReturnText,
	string? ErrorText,
	TimeSpan Elapsed,
	bool IsUnloaded);
