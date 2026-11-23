namespace InventorAddinManager.InventorTests;

/// <summary>
/// 	Runs commands in a live Inventor through the manager, and checks what the README promises.
/// </summary>
/// <remarks>
/// 	Before a run: build this configuration, deploy the manager for the same release, and start that Inventor.
/// 	No document needs to be open.
/// 	The tests share one Inventor. xUnit runs the tests of one class one at a time, so keep them in this class.
/// </remarks>
[Trait("Level", "Inventor")]
public sealed class ManagerSmokeTests(InventorSession session) : IClassFixture<InventorSession>
{
	private const string _commandsNamespace = "InventorAddinManager.Tests.Commands";

	[Fact]
	public void Manager_IsLoadedWithItsButtons()
	{
		_ = session.GetManagerAutomation();

		Inventor.ControlDefinitions definitions = session.Application!.CommandManager.ControlDefinitions;
		Assert.NotNull(definitions["InventorAddinManager:Manager"]);
		Assert.NotNull(definitions["InventorAddinManager:RunLast"]);
	}

	[Fact]
	public void ListCommands_TestCommands_ListsTheCommands()
	{
		dynamic automation = session.GetManagerAutomation();

		object[] commands = automation.ListCommands(InventorSession.CommandsAssemblyPath);

		Assert.Contains($"{_commandsNamespace}.StaticCommand", commands.Cast<string>());
		Assert.DoesNotContain($"{_commandsNamespace}.NoEntryMethod", commands.Cast<string>());
	}

	[Fact]
	public void Run_SessionCommand_GetsTheRunningInventorAndUnloads()
	{
		dynamic automation = session.GetManagerAutomation();

		dynamic result = automation.Run(InventorSession.CommandsAssemblyPath, $"{_commandsNamespace}.SessionCommand", false);

		Assert.True((bool)result.Succeeded, (string?)result.ErrorText);
		Assert.Equal(session.Application!.SoftwareVersion.DisplayVersion, (string?)result.ReturnText);
		Assert.True((bool)result.IsUnloaded);
	}

	[Fact]
	public void Run_TwoRuns_StartWithEmptyStaticFields()
	{
		dynamic automation = session.GetManagerAutomation();

		dynamic first = automation.Run(InventorSession.CommandsAssemblyPath, $"{_commandsNamespace}.RunCounterCommand", false);
		dynamic second = automation.Run(InventorSession.CommandsAssemblyPath, $"{_commandsNamespace}.RunCounterCommand", false);

		Assert.Equal("1", (string?)first.ReturnText);
		Assert.Equal("1", (string?)second.ReturnText);
	}

	[Fact]
	public void Run_FailingCommand_ReportsTheException()
	{
		dynamic automation = session.GetManagerAutomation();

		dynamic result = automation.Run(InventorSession.CommandsAssemblyPath, $"{_commandsNamespace}.FailingCommand", false);

		Assert.False((bool)result.Succeeded);
		Assert.Contains("The fixture command failed.", (string?)result.ErrorText);
	}

	[Fact]
	public void Run_NewtonsoftInInventor_CommandGetsItsOwnVersion()
	{
		dynamic automation = session.GetManagerAutomation();

		dynamic result = automation.Run(InventorSession.CommandsAssemblyPath, $"{_commandsNamespace}.NewtonsoftVersionCommand", false);

		Assert.True((bool)result.Succeeded, (string?)result.ErrorText);
		Assert.StartsWith("13.0.4", (string?)result.ReturnText);
	}

	[Fact]
	public void Run_SampleActiveDocumentReport_Succeeds()
	{
		dynamic automation = session.GetManagerAutomation();

		dynamic result = automation.Run(InventorSession.SampleCommandsAssemblyPath, "SampleCommands.ActiveDocumentReport", false);

		Assert.True((bool)result.Succeeded, (string?)result.ErrorText);
		Assert.True((bool)result.IsUnloaded);
	}
}
