using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Windows.Interop;
using Inventor;
using InventorAddinManager.Diagnostics;
using InventorAddinManager.Loading;
using InventorAddinManager.Ribbon;
using InventorAddinManager.Settings;
using InventorAddinManager.Views;

namespace InventorAddinManager;

/// <summary>
/// 	Entry point that Inventor loads through the .addin manifest.
/// </summary>
/// <remarks>
/// 	This assembly stays loaded until Inventor closes, so a change here needs an Inventor restart.
/// 	Keep it small. The commands that it runs are the code that changes often.
/// </remarks>
[Guid(_classGuid)]
[ComVisible(true)]
public sealed class AddInServer : ApplicationAddInServer
{
	/// <summary>
	/// 	The client ID that owns the buttons and the ribbon panels. It is the same as the ClassId in the manifest.
	/// </summary>
	public const string ClientId = "{" + _classGuid + "}";

	private const string _classGuid = "4421BA74-4DBD-4925-9521-35C05DD70634";
	private const string _panelInternalName = "InventorAddinManager:Panel";
	private const string _toolsTabInternalName = "id_TabTools";

	private static readonly string[] _ribbonNames = ["ZeroDoc", "Part", "Assembly", "Drawing", "Presentation"];

	private readonly List<RibbonPanel> _ribbonPanels = [];

	private Inventor.Application? _inventorApplication;
	private ManagerSettings? _settings;
	private CommandRunner? _commandRunner;
	private RibbonButton? _managerButton;
	private RibbonButton? _runLastButton;
	private UserInterfaceEvents? _userInterfaceEvents;
	private UserInterfaceEventsSink_OnResetRibbonInterfaceEventHandler? _onResetRibbonInterfaceHandler;

	public object? Automation => null;

	public void Activate(ApplicationAddInSite AddInSiteObject, bool FirstTime)
	{
		_inventorApplication = AddInSiteObject.Application;
		_settings = ManagerSettings.Load(_inventorApplication.SoftwareVersion.Major);
		_commandRunner = new CommandRunner(_inventorApplication);

		ManagerLog.DeleteOldFiles();
		ManagerLog.Write(string.Create(
			CultureInfo.InvariantCulture,
			$"Activate: Inventor {_inventorApplication.SoftwareVersion.DisplayVersion}, first time {FirstTime}, load context '{AssemblyLoadContext.GetLoadContext(typeof(AddInServer).Assembly)?.Name}'"));

		ControlDefinitions controlDefinitions = _inventorApplication.CommandManager.ControlDefinitions;
		_managerButton = new RibbonButton(
			controlDefinitions,
			"Add-In\nManager",
			"InventorAddinManager:Manager",
			"Pick a build output and run one of its commands.",
			"Manager",
			ShowManager);
		_runLastButton = new RibbonButton(
			controlDefinitions,
			"Run\nLast",
			"InventorAddinManager:RunLast",
			"Run the last command again from its newest build.",
			"RunLast",
			RunLastCommand);

		_userInterfaceEvents = _inventorApplication.UserInterfaceManager.UserInterfaceEvents;
		_onResetRibbonInterfaceHandler = OnResetRibbonInterface;
		_userInterfaceEvents.OnResetRibbonInterface += _onResetRibbonInterfaceHandler;

		// Always, not only on FirstTime: Deactivate deletes the panels, so Inventor has none to restore.
		AddRibbonPanels();
	}

	public void Deactivate()
	{
		ManagerLog.Write("Deactivate");

		if (_userInterfaceEvents is not null && _onResetRibbonInterfaceHandler is not null)
		{
			_userInterfaceEvents.OnResetRibbonInterface -= _onResetRibbonInterfaceHandler;
		}

		RemoveRibbonPanels();
		_managerButton?.Dispose();
		_runLastButton?.Dispose();
		_managerButton = null;
		_runLastButton = null;

		_userInterfaceEvents = null;
		_onResetRibbonInterfaceHandler = null;
		_commandRunner = null;
		_inventorApplication = null;

		// Finalize the event providers of the released buttons now, before a later Activate creates new ones.
		// A provider finalized later releases a connection point that Inventor can give to the new button with
		// the same internal name, which disconnects the new OnExecute handler with no error.
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	[Obsolete("Inventor no longer calls this member. The ApplicationAddInServer interface still requires it.")]
	public void ExecuteCommand(int CommandID)
	{
	}

	private void ShowManager()
	{
		ManagerWindow window = new(_settings!);
		_ = new WindowInteropHelper(window) { Owner = new IntPtr(_inventorApplication!.MainFrameHWND) };

		bool? accepted = window.ShowDialog();
		_settings!.Save();
		ManagerLog.Write($"Manager window closed: {(accepted is true ? $"run {window.SelectedCommand?.TypeName}" : "no command")}");

		if (accepted is true && window.SelectedCommand is CommandReference command)
		{
			RunCommand(command);
		}
	}

	private void RunLastCommand()
	{
		if (_settings!.LastCommand is CommandReference command)
		{
			RunCommand(command);
		}
		else
		{
			ShowManager();
		}
	}

	private void RunCommand(CommandReference command)
	{
		_settings!.LastCommand = command;
		_settings.Save();

		CommandRunResult result = _commandRunner!.Run(command, _settings.WrapInTransaction);
		ManagerLog.Write(string.Create(
			CultureInfo.InvariantCulture,
			$"Run: {command.TypeName} from '{command.AssemblyPath}', succeeded {result.Succeeded}, {result.Elapsed.TotalMilliseconds:F0} ms, unloaded {result.IsUnloaded}"));
		if (result.ErrorText is not null)
		{
			ManagerLog.Write($"Run error: {result.ErrorText}");
		}

		if (ResultWindow.IsWorthShowing(result) is false)
		{
			return;
		}

		ResultWindow window = ResultWindow.Create(result);
		_ = new WindowInteropHelper(window) { Owner = new IntPtr(_inventorApplication!.MainFrameHWND) };
		_ = window.ShowDialog();
	}

	private void OnResetRibbonInterface(NameValueMap context)
	{
		RemoveRibbonPanels();
		AddRibbonPanels();
	}

	private void AddRibbonPanels()
	{
		UserInterfaceManager userInterfaceManager = _inventorApplication!.UserInterfaceManager;
		if (userInterfaceManager.InterfaceStyle != InterfaceStyleEnum.kRibbonInterface)
		{
			return;
		}

		foreach (string ribbonName in _ribbonNames)
		{
			if (FindToolsTab(userInterfaceManager, ribbonName) is not RibbonTab toolsTab)
			{
				continue;
			}

			// A session that ends without Deactivate, ex. a crash, can leave the panel in the saved ribbon.
			DeletePanel(toolsTab);

			RibbonPanel panel = toolsTab.RibbonPanels.Add("Add-In Manager", _panelInternalName, ClientId);
			_ = panel.CommandControls.AddButton(_managerButton!.Definition, true);
			_ = panel.CommandControls.AddButton(_runLastButton!.Definition, true);
			_ribbonPanels.Add(panel);
		}
	}

	private void RemoveRibbonPanels()
	{
		foreach (RibbonPanel panel in _ribbonPanels)
		{
			try
			{
				panel.Delete();
			}
			catch (COMException)
			{
				// A ribbon reset can delete the panel first.
			}
		}

		_ribbonPanels.Clear();
	}

	/// <returns>
	/// 	The Tools tab of the ribbon, or <see langword="null" /> when the ribbon or the tab does not exist.
	/// </returns>
	private static RibbonTab? FindToolsTab(UserInterfaceManager userInterfaceManager, string ribbonName)
	{
		try
		{
			Inventor.Ribbon ribbon = userInterfaceManager.Ribbons[ribbonName];
			return ribbon.RibbonTabs.OfType<RibbonTab>().FirstOrDefault(static tab => tab.InternalName == _toolsTabInternalName);
		}
		catch (COMException)
		{
			return null;
		}
	}

	private static void DeletePanel(RibbonTab toolsTab)
	{
		foreach (RibbonPanel panel in toolsTab.RibbonPanels.OfType<RibbonPanel>().Where(static panel => panel.InternalName == _panelInternalName).ToList())
		{
			panel.Delete();
		}
	}
}
