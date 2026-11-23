using System.Diagnostics;
using System.Runtime.Loader;
using InventorAddinManager.Loading;
using Newtonsoft.Json;

namespace InventorAddinManager.Tests;

/// <summary>
/// 	Runs the test commands through the manager with no Inventor session.
/// </summary>
/// <remarks>
/// 	The session is <see langword="null" />, and no run uses a transaction, so no run makes a COM call.
/// </remarks>
public sealed class CommandRunnerTests : IDisposable
{
	private readonly TemporaryFolder _shadowRoot = new();
	private readonly CommandRunner _runner;

	public CommandRunnerTests()
	{
		_runner = new CommandRunner(null!, _shadowRoot.Path);
	}

	public void Dispose()
	{
		_shadowRoot.Dispose();
	}

	[Fact]
	public void Run_StaticCommand_ReturnsItsTextAndUnloads()
	{
		CommandRunResult result = Run("StaticCommand");

		Assert.True(result.Succeeded, result.ErrorText);
		Assert.Equal("static", result.ReturnText);
		Assert.True(result.IsUnloaded);
	}

	[Fact]
	public void Run_InstanceCommand_GetsTheSessionThroughItsConstructor()
	{
		CommandRunResult result = Run("SessionCommand");

		Assert.True(result.Succeeded, result.ErrorText);
		Assert.Equal("no session", result.ReturnText);
	}

	[Fact]
	public void Run_ParameterWithDefaultValue_GetsTheDefault()
	{
		CommandRunResult result = Run("DefaultValueCommand");

		Assert.Equal("3", result.ReturnText);
	}

	[Fact]
	public void Run_FailingCommand_ReportsTheInnerExceptionAndUnloads()
	{
		CommandRunResult result = Run("FailingCommand");

		Assert.False(result.Succeeded);
		Assert.Null(result.ReturnText);
		Assert.StartsWith("System.InvalidOperationException: The fixture command failed.", result.ErrorText);
		Assert.True(result.IsUnloaded);
	}

	[Fact]
	public void Run_TwoRuns_StartWithEmptyStaticFields()
	{
		CommandRunResult first = Run("RunCounterCommand");
		CommandRunResult second = Run("RunCounterCommand");

		Assert.Equal("1", first.ReturnText);
		Assert.Equal("1", second.ReturnText);
	}

	[Fact]
	public void Run_CommandThatKeepsAReference_IsReportedAsNotUnloaded()
	{
		const string dataName = "InventorAddinManager.Tests.LeakingCommand";
		try
		{
			CommandRunResult result = Run("LeakingCommand");

			Assert.True(result.Succeeded, result.ErrorText);
			Assert.False(result.IsUnloaded);
		}
		finally
		{
			AppDomain.CurrentDomain.SetData(dataName, null);
		}
	}

	[Fact]
	public void Run_AssemblyLocation_IsTheShadowCopy()
	{
		CommandRunResult result = Run("LocationCommand");

		Assert.NotNull(result.ReturnText);
		Assert.StartsWith(_shadowRoot.Path, result.ReturnText);
		Assert.True(File.Exists(result.ReturnText));
	}

	[Fact]
	public void Run_PackageThatTheDefaultContextHasInAnOlderVersion_LoadsTheCommandsOwnCopy()
	{
		// The default context gets 13.0.3 first, as it does in Inventor.
		string defaultContextVersion = FileVersionInfo.GetVersionInfo(typeof(JsonConvert).Assembly.Location).FileVersion!;
		Assert.StartsWith("13.0.3", defaultContextVersion);

		CommandRunResult result = Run("NewtonsoftVersionCommand");

		Assert.True(result.Succeeded, result.ErrorText);
		string[] parts = result.ReturnText!.Split('|');
		Assert.StartsWith("13.0.4", parts[0]);
		Assert.StartsWith(_shadowRoot.Path, parts[1]);
	}

	[Fact]
	public void Run_InventorInterop_IsTheManagersCopy()
	{
		string? managerContext = AssemblyLoadContext.GetLoadContext(typeof(Inventor.Application).Assembly)?.Name;

		CommandRunResult result = Run("InteropContextCommand");

		Assert.True(result.Succeeded, result.ErrorText);
		Assert.Equal(managerContext, result.ReturnText);
	}

	[Fact]
	public void Run_TypeNotInTheBuild_FailsWithAClearMessage()
	{
		CommandRunResult result = Run("RemovedCommand");

		Assert.False(result.Succeeded);
		Assert.Contains("has no public type", result.ErrorText);
	}

	[Fact]
	public void Run_MissingBuild_FailsWithAClearMessage()
	{
		CommandRunResult result = _runner.Run(new CommandReference(Path.Combine(_shadowRoot.Path, "Missing.dll"), "Missing.Command"), wrapInTransaction: false);

		Assert.False(result.Succeeded);
		Assert.Contains("Build the project first", result.ErrorText);
	}

	private CommandRunResult Run(string shortName)
	{
		return _runner.Run(new CommandReference(TestCommands.AssemblyPath, TestCommands.TypeName(shortName)), wrapInTransaction: false);
	}
}
