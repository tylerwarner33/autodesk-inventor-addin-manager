using InventorAddinManager.Loading;

namespace InventorAddinManager.Tests;

public sealed class ShadowCopyTests : IDisposable
{
	private readonly TemporaryFolder _buildOutput = new();
	private readonly TemporaryFolder _shadowRoot = new();
	private readonly string _assemblyPath;

	public ShadowCopyTests()
	{
		_assemblyPath = Path.Combine(_buildOutput.Path, "MyCommands.dll");
		File.WriteAllText(_assemblyPath, "assembly");
		File.WriteAllText(Path.Combine(_buildOutput.Path, "MyCommands.deps.json"), "{}");

		string resourceFolder = Path.Combine(_buildOutput.Path, "de");
		_ = Directory.CreateDirectory(resourceFolder);
		File.WriteAllText(Path.Combine(resourceFolder, "MyCommands.resources.dll"), "resources");
	}

	public void Dispose()
	{
		_buildOutput.Dispose();
		_shadowRoot.Dispose();
	}

	[Fact]
	public void Create_BuildOutput_CopiesEveryFileAndSubfolder()
	{
		string shadowAssemblyPath = ShadowCopy.Create(_assemblyPath, _shadowRoot.Path);
		string shadowFolder = Path.GetDirectoryName(shadowAssemblyPath)!;

		Assert.Equal("MyCommands.dll", Path.GetFileName(shadowAssemblyPath));
		Assert.Equal("assembly", File.ReadAllText(shadowAssemblyPath));
		Assert.True(File.Exists(Path.Combine(shadowFolder, "MyCommands.deps.json")));
		Assert.True(File.Exists(Path.Combine(shadowFolder, "de", "MyCommands.resources.dll")));
	}

	[Fact]
	public void Create_ShadowFolder_IsBelowRootInAFolderForTheAssembly()
	{
		string shadowAssemblyPath = ShadowCopy.Create(_assemblyPath, _shadowRoot.Path);

		string assemblyShadowRoot = Path.GetDirectoryName(Path.GetDirectoryName(shadowAssemblyPath))!;
		Assert.Equal(Path.Combine(_shadowRoot.Path, "MyCommands"), assemblyShadowRoot);
	}

	[Fact]
	public void Create_TwoRuns_GiveTwoFolders()
	{
		string first = ShadowCopy.Create(_assemblyPath, _shadowRoot.Path);
		string second = ShadowCopy.Create(_assemblyPath, _shadowRoot.Path);

		Assert.NotEqual(Path.GetDirectoryName(first), Path.GetDirectoryName(second));
	}

	[Fact]
	public void Create_CopyOlderThanMaximumAge_IsDeleted()
	{
		string oldFolder = Path.GetDirectoryName(ShadowCopy.Create(_assemblyPath, _shadowRoot.Path))!;
		Directory.SetCreationTimeUtc(oldFolder, DateTime.UtcNow - ShadowCopy.MaximumAge - TimeSpan.FromHours(1));

		string newFolder = Path.GetDirectoryName(ShadowCopy.Create(_assemblyPath, _shadowRoot.Path))!;

		Assert.False(Directory.Exists(oldFolder));
		Assert.True(Directory.Exists(newFolder));
	}

	[Fact]
	public void Create_RecentCopy_IsKept()
	{
		string recentFolder = Path.GetDirectoryName(ShadowCopy.Create(_assemblyPath, _shadowRoot.Path))!;

		_ = ShadowCopy.Create(_assemblyPath, _shadowRoot.Path);

		Assert.True(Directory.Exists(recentFolder));
	}
}
