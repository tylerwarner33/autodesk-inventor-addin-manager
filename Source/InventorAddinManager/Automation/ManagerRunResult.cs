using System.Runtime.InteropServices;
using InventorAddinManager.Loading;

namespace InventorAddinManager.Automation;

/// <summary>
/// 	The result of one run through <see cref="ManagerAutomation" />.
/// </summary>
/// <remarks>
/// 	It holds only strings and values, so it does not keep the build loaded.
/// </remarks>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
public sealed class ManagerRunResult
{
	internal ManagerRunResult(CommandRunResult result)
	{
		Succeeded = result.Succeeded;
		ReturnText = result.ReturnText;
		ErrorText = result.ErrorText;
		ElapsedMilliseconds = result.Elapsed.TotalMilliseconds;
		IsUnloaded = result.IsUnloaded;
	}

	/// <summary>
	/// 	Gets a value that tells if the command returned without an exception.
	/// </summary>
	public bool Succeeded { get; }

	/// <summary>
	/// 	Gets the text of the value that the command returned, or <see langword="null" />.
	/// </summary>
	public string? ReturnText { get; }

	/// <summary>
	/// 	Gets the exception text, or <see langword="null" /> when the command succeeded.
	/// </summary>
	public string? ErrorText { get; }

	/// <summary>
	/// 	Gets the time to copy, load, and run the command.
	/// </summary>
	public double ElapsedMilliseconds { get; }

	/// <summary>
	/// 	Gets a value that tells if the build left memory after the run.
	/// </summary>
	public bool IsUnloaded { get; }
}
