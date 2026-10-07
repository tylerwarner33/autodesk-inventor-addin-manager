using System.Globalization;
using System.Text;
using System.Windows;
using InventorAddinManager.Loading;

namespace InventorAddinManager.Views;

/// <summary>
/// 	Shows what a command returned, its exception, or a build that stays loaded.
/// </summary>
/// <remarks>
/// 	A text box, not a message box, so the user can copy a stack trace.
/// </remarks>
public partial class ResultWindow : Window
{
	private ResultWindow(string title, string text)
	{
		InitializeComponent();

		Title = title;
		_resultText.Text = text;
	}

	/// <summary>
	/// 	Gets a value that tells if the result has something to show.
	/// </summary>
	/// <remarks>
	/// 	A command that succeeds, returns nothing, and unloads shows nothing, so the edit, build, and run loop stays fast.
	/// </remarks>
	internal static bool IsWorthShowing(CommandRunResult result)
	{
		return result.Succeeded is false || result.ReturnText is not null || result.IsUnloaded is false;
	}

	/// <summary>
	/// 	Creates the window for a result.
	/// </summary>
	internal static ResultWindow Create(CommandRunResult result)
	{
		StringBuilder text = new();
		string seconds = result.Elapsed.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture);

		if (result.Succeeded)
		{
			_ = text.AppendLine($"{result.Command.TypeName} ran in {seconds} s.");
			if (result.ReturnText is not null)
			{
				_ = text.AppendLine().AppendLine(result.ReturnText);
			}
		}
		else
		{
			_ = text.AppendLine($"{result.Command.TypeName} failed after {seconds} s.");
			_ = text.AppendLine().AppendLine(result.ErrorText);
		}

		if (result.IsUnloaded is false)
		{
			_ = text.AppendLine()
				.AppendLine("The build is still loaded after the run.")
				.AppendLine("Something still holds a reference into it, ex. an event handler, a static field in another assembly, or an open window.")
				.AppendLine("Each run then adds one more copy to memory until Inventor closes.");
		}

		string title = result.Succeeded ? $"Add-In Manager: {result.Command.ShortName}" : $"Add-In Manager: {result.Command.ShortName} failed";

		return new ResultWindow(title, text.ToString());
	}
}
