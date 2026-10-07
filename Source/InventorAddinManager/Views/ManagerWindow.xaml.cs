using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using InventorAddinManager.Diagnostics;
using InventorAddinManager.Loading;
using InventorAddinManager.Settings;
using Microsoft.Win32;

namespace InventorAddinManager.Views;

/// <summary>
/// 	Lets the user pick a build output and a command in it.
/// </summary>
/// <remarks>
/// 	The window is modal and closes before the command runs.
/// 	The command can then use Inventor's selection and interaction, which a modal window would block.
/// </remarks>
public partial class ManagerWindow : Window
{
	private readonly ManagerSettings _settings;

	/// <summary>
	/// 	Creates the window and fills it from the settings.
	/// </summary>
	/// <param name="settings">
	/// 	The settings that the window reads and changes. The caller saves them.
	/// </param>
	public ManagerWindow(ManagerSettings settings)
	{
		InitializeComponent();

		_settings = settings;
		_wrapInTransactionCheckBox.IsChecked = settings.WrapInTransaction;

		foreach (string assemblyPath in settings.AssemblyPaths)
		{
			_ = _assemblyList.Items.Add(new BuildOutputItem(assemblyPath));
		}

		string? selectedPath = settings.LastCommand is CommandReference lastCommand && settings.AssemblyPaths.Contains(lastCommand.AssemblyPath)
			? lastCommand.AssemblyPath
			: settings.AssemblyPaths.FirstOrDefault();
		SelectAssembly(selectedPath);
	}

	/// <summary>
	/// 	Gets the command to run, after the user clicks <b>Run</b>.
	/// </summary>
	public CommandReference? SelectedCommand { get; private set; }

	protected override void OnClosing(CancelEventArgs e)
	{
		_settings.WrapInTransaction = _wrapInTransactionCheckBox.IsChecked is true;
		base.OnClosing(e);
	}

	private void AddButton_Click(object sender, RoutedEventArgs e)
	{
		OpenFileDialog dialog = new()
		{
			Title = "Select the assembly in a build output",
			Filter = "Assemblies (*.dll)|*.dll",
		};

		if (dialog.ShowDialog(this) is not true)
		{
			return;
		}

		string assemblyPath = dialog.FileName;
		if (_settings.AssemblyPaths.Contains(assemblyPath, StringComparer.OrdinalIgnoreCase) is false)
		{
			_settings.AssemblyPaths.Add(assemblyPath);
			_ = _assemblyList.Items.Add(new BuildOutputItem(assemblyPath));
		}

		SelectAssembly(assemblyPath);
	}

	private void RemoveButton_Click(object sender, RoutedEventArgs e)
	{
		if (_assemblyList.SelectedItem is not BuildOutputItem item)
		{
			return;
		}

		_ = _settings.AssemblyPaths.RemoveAll(path => string.Equals(path, item.AssemblyPath, StringComparison.OrdinalIgnoreCase));
		_assemblyList.Items.Remove(item);
		SelectAssembly(_settings.AssemblyPaths.FirstOrDefault());
	}

	private void RefreshButton_Click(object sender, RoutedEventArgs e)
	{
		if (_assemblyList.SelectedItem is BuildOutputItem item)
		{
			ShowCommands(item.AssemblyPath);
		}
	}

	private void AssemblyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ShowSelectedAssembly();
	}

	private void ShowSelectedAssembly()
	{
		bool hasSelection = _assemblyList.SelectedItem is BuildOutputItem;
		_removeButton.IsEnabled = hasSelection;
		_refreshButton.IsEnabled = hasSelection;

		if (_assemblyList.SelectedItem is BuildOutputItem item)
		{
			ShowCommands(item.AssemblyPath);
		}
		else
		{
			_commandList.ItemsSource = null;
			_statusText.Text = "Click Add to select the assembly in a build output, ex. bin\\Debug\\2027\\MyAddin.dll.";
		}
	}

	private void CommandList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		_runButton.IsEnabled = _commandList.SelectedItem is CommandReference;
	}

	private void CommandList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (_commandList.SelectedItem is CommandReference)
		{
			RunSelectedCommand();
		}
	}

	private void RunButton_Click(object sender, RoutedEventArgs e)
	{
		RunSelectedCommand();
	}

	private void RunSelectedCommand()
	{
		SelectedCommand = _commandList.SelectedItem as CommandReference;
		DialogResult = SelectedCommand is not null;
	}

	/// <summary>
	/// 	Selects the build output with the path, or clears the selection when the path is <see langword="null" />.
	/// </summary>
	private void SelectAssembly(string? assemblyPath)
	{
		_assemblyList.SelectedItem = _assemblyList.Items
			.OfType<BuildOutputItem>()
			.FirstOrDefault(item => string.Equals(item.AssemblyPath, assemblyPath, StringComparison.OrdinalIgnoreCase));

		// No SelectionChanged event when nothing was selected before and nothing is selected now.
		if (_assemblyList.SelectedItem is null)
		{
			ShowSelectedAssembly();
		}
	}

	/// <summary>
	/// 	Reads the commands from the newest build and selects the last command when it is in the list.
	/// </summary>
	private void ShowCommands(string assemblyPath)
	{
		_commandList.ItemsSource = null;

		IReadOnlyList<CommandReference> commands;
		try
		{
			Cursor = Cursors.Wait;
			commands = CommandCatalog.Discover(assemblyPath);
		}
		catch (Exception exception)
		{
			ManagerLog.Write($"Cannot read '{assemblyPath}'", exception);
			_statusText.Text = $"Cannot read the build: {exception.Message}";
			return;
		}
		finally
		{
			Cursor = null;
		}

		_commandList.ItemsSource = commands;
		_commandList.SelectedItem = commands.FirstOrDefault(command => command == _settings.LastCommand) ?? commands.FirstOrDefault();
		_statusText.Text = commands.Count == 0
			? $"No commands. A command is a public class with a public '{CommandBinder.EntryMethodName}' method."
			: $"Commands found: {commands.Count}. Click Refresh after a build that adds or removes a command.";
	}
}
