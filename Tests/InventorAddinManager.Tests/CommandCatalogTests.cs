using InventorAddinManager.Loading;

namespace InventorAddinManager.Tests;

public sealed class CommandCatalogTests : IDisposable
{
	private readonly TemporaryFolder _shadowRoot = new();

	public void Dispose()
	{
		_shadowRoot.Dispose();
	}

	[Fact]
	public void Discover_TestCommands_ListsOnlyTheCommandsSorted()
	{
		IReadOnlyList<CommandReference> commands = CommandCatalog.Discover(TestCommands.AssemblyPath, _shadowRoot.Path);

		string[] expected =
		[
			TestCommands.TypeName("DefaultValueCommand"),
			TestCommands.TypeName("FailingCommand"),
			TestCommands.TypeName("InteropContextCommand"),
			TestCommands.TypeName("LeakingCommand"),
			TestCommands.TypeName("LocationCommand"),
			TestCommands.TypeName("NewtonsoftVersionCommand"),
			TestCommands.TypeName("RunCounterCommand"),
			TestCommands.TypeName("SessionCommand"),
			TestCommands.TypeName("StaticCommand"),
		];
		Assert.Equal(expected, commands.Select(static command => command.TypeName));
	}

	[Fact]
	public void Discover_TestCommands_KeepsTheBuildOutputPath()
	{
		IReadOnlyList<CommandReference> commands = CommandCatalog.Discover(TestCommands.AssemblyPath, _shadowRoot.Path);

		Assert.All(commands, static command => Assert.Equal(TestCommands.AssemblyPath, command.AssemblyPath));
	}

	[Fact]
	public void Discover_MissingAssembly_Throws()
	{
		string missingPath = Path.Combine(_shadowRoot.Path, "Missing.dll");

		_ = Assert.Throws<FileNotFoundException>(() => CommandCatalog.Discover(missingPath, _shadowRoot.Path));
	}
}
