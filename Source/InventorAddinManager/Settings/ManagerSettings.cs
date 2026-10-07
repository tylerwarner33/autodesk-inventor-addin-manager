using System.IO;
using System.Text.Json;
using InventorAddinManager.Loading;

namespace InventorAddinManager.Settings;

/// <summary>
/// 	The build outputs and the last command, kept between Inventor sessions.
/// </summary>
/// <remarks>
/// 	Each Inventor release has its own file, because each release needs a build for its own runtime.
/// </remarks>
public sealed class ManagerSettings
{
	private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

	private string _filePath = string.Empty;

	/// <summary>
	/// 	Gets or sets the paths of the assemblies in the manager list.
	/// </summary>
	public List<string> AssemblyPaths { get; set; } = [];

	/// <summary>
	/// 	Gets or sets the command that <b>Run Last</b> runs.
	/// </summary>
	public CommandReference? LastCommand { get; set; }

	/// <summary>
	/// 	Gets or sets a value that tells if a run is one undo step.
	/// </summary>
	public bool WrapInTransaction { get; set; } = true;

	/// <summary>
	/// 	Reads the settings for an Inventor release.
	/// </summary>
	/// <param name="softwareMajorVersion">
	/// 	The major version that Inventor reports, ex. 31 for Inventor 2027.
	/// </param>
	/// <returns>
	/// 	The saved settings, or new settings when the file does not exist or cannot be read.
	/// </returns>
	public static ManagerSettings Load(int softwareMajorVersion)
	{
		string filePath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			nameof(InventorAddinManager),
			$"settings.{softwareMajorVersion}.json");

		ManagerSettings? settings = null;
		try
		{
			if (File.Exists(filePath))
			{
				settings = JsonSerializer.Deserialize<ManagerSettings>(File.ReadAllText(filePath), _jsonOptions);
			}
		}
		catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
		{
			// A damaged file must not stop the add-in. The next save replaces it.
		}

		settings ??= new ManagerSettings();
		settings._filePath = filePath;

		return settings;
	}

	/// <summary>
	/// 	Writes the settings to the file that <see cref="Load" /> read.
	/// </summary>
	public void Save()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
		File.WriteAllText(_filePath, JsonSerializer.Serialize(this, _jsonOptions));
	}
}
