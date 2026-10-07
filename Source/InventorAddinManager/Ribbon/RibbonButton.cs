using System.Runtime.InteropServices;
using Inventor;
using InventorAddinManager.Diagnostics;

namespace InventorAddinManager.Ribbon;

/// <summary>
/// 	A button definition and the handler for its <c>OnExecute</c> event.
/// </summary>
internal sealed class RibbonButton : IDisposable
{
	private readonly string _internalName;
	private readonly Action _execute;
	private readonly ButtonDefinitionSink_OnExecuteEventHandler _onExecuteHandler;
	private ButtonDefinition? _buttonDefinition;

	/// <summary>
	/// 	Creates the button definition.
	/// </summary>
	/// <param name="controlDefinitions">
	/// 	The control definitions of the Inventor session.
	/// </param>
	/// <param name="displayName">
	/// 	The text on the button. A line break splits it on a large button.
	/// </param>
	/// <param name="internalName">
	/// 	A name that is unique in the Inventor session.
	/// </param>
	/// <param name="description">
	/// 	The description and the tooltip.
	/// </param>
	/// <param name="iconName">
	/// 	The icon name in the Resources folder, without the size and extension.
	/// 	The folder must have <c>&lt;iconName&gt;16.png</c> and <c>&lt;iconName&gt;32.png</c>.
	/// </param>
	/// <param name="execute">
	/// 	The action that a click starts.
	/// </param>
	public RibbonButton(ControlDefinitions controlDefinitions, string displayName, string internalName, string description, string iconName, Action execute)
	{
		_internalName = internalName;
		_execute = execute;
		_buttonDefinition = controlDefinitions.AddButtonDefinition(
			displayName,
			internalName,
			CommandTypesEnum.kNonShapeEditCmdType,
			AddInServer.ClientId,
			description,
			description,
			RibbonIcons.Load($"{iconName}16.png"),
			RibbonIcons.Load($"{iconName}32.png"),
			ButtonDisplayEnum.kAlwaysDisplayText);

		// The delegate is kept in a field, so the same instance can unsubscribe in Dispose.
		_onExecuteHandler = OnExecute;
		_buttonDefinition.OnExecute += _onExecuteHandler;
	}

	/// <summary>
	/// 	Gets the button definition to add to a ribbon panel.
	/// </summary>
	public ButtonDefinition Definition => _buttonDefinition ?? throw new ObjectDisposedException(nameof(RibbonButton));

	public void Dispose()
	{
		if (_buttonDefinition is null)
		{
			return;
		}

		try
		{
			_buttonDefinition.OnExecute -= _onExecuteHandler;
			_buttonDefinition.Delete();
		}
		catch (COMException)
		{
			// Inventor can remove the definition first while it shuts down.
		}
		finally
		{
			Marshal.ReleaseComObject(_buttonDefinition);
			_buttonDefinition = null;
		}
	}

	private void OnExecute(NameValueMap context)
	{
		ManagerLog.Write($"Click: {_internalName}");
		try
		{
			_execute();
		}
		catch (Exception exception)
		{
			ManagerLog.Write($"Click failed: {_internalName}", exception);
			// An exception that reaches Inventor from an event handler is lost, so show it here.
			System.Windows.MessageBox.Show(exception.ToString(), "Add-In Manager", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
		}
	}
}
