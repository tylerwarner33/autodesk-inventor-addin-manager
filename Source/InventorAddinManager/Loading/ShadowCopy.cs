using System.Globalization;
using System.IO;

namespace InventorAddinManager.Loading;

/// <summary>
/// 	Copies a build output to a new folder, so Inventor never locks the build output itself.
/// </summary>
/// <remarks>
/// 	The whole folder is copied, not only the assembly, so dependencies and files beside the assembly come too.
/// 	<see cref="System.Reflection.Assembly.Location" /> in the command then gives a real path in the copy.
/// </remarks>
internal static class ShadowCopy
{
	/// <summary>
	/// 	The folder that holds the shadow copies of all builds.
	/// </summary>
	public static readonly string DefaultRoot = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		nameof(InventorAddinManager),
		"Shadow");

	/// <summary>
	/// 	The age after which <see cref="Create" /> deletes a shadow copy.
	/// </summary>
	public static readonly TimeSpan MaximumAge = TimeSpan.FromDays(1);

	/// <summary>
	/// 	Copies the folder that holds the assembly to a new shadow folder.
	/// </summary>
	/// <param name="assemblyPath">
	/// 	Path of the assembly in the build output.
	/// </param>
	/// <param name="shadowRoot">
	/// 	The folder for the shadow copies, or <see langword="null" /> for <see cref="DefaultRoot" />.
	/// 	The tests give a temporary folder.
	/// </param>
	/// <returns>
	/// 	Path of the assembly in the shadow copy.
	/// </returns>
	public static string Create(string assemblyPath, string? shadowRoot = null)
	{
		string sourceDirectory = Path.GetDirectoryName(assemblyPath)
			?? throw new ArgumentException($"'{assemblyPath}' has no folder.", nameof(assemblyPath));

		string assemblyShadowRoot = Path.Combine(shadowRoot ?? DefaultRoot, Path.GetFileNameWithoutExtension(assemblyPath));
		DeleteOldCopies(assemblyShadowRoot);

		string folderName = $"{DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture)}-{Guid.NewGuid().ToString("N")[..6]}";
		string targetDirectory = Path.Combine(assemblyShadowRoot, folderName);
		CopyDirectory(sourceDirectory, targetDirectory);

		return Path.Combine(targetDirectory, Path.GetFileName(assemblyPath));
	}

	private static void CopyDirectory(string sourceDirectory, string targetDirectory)
	{
		Directory.CreateDirectory(targetDirectory);

		foreach (string file in Directory.GetFiles(sourceDirectory))
		{
			File.Copy(file, Path.Combine(targetDirectory, Path.GetFileName(file)));
		}

		foreach (string directory in Directory.GetDirectories(sourceDirectory))
		{
			CopyDirectory(directory, Path.Combine(targetDirectory, Path.GetFileName(directory)));
		}
	}

	/// <summary>
	/// 	Deletes shadow copies older than a day.
	/// </summary>
	/// <remarks>
	/// 	Best effort: a copy that a context still holds open is left for a later run.
	/// </remarks>
	private static void DeleteOldCopies(string assemblyShadowRoot)
	{
		if (Directory.Exists(assemblyShadowRoot) is false)
		{
			return;
		}

		DateTime oldestAllowed = DateTime.UtcNow - MaximumAge;
		foreach (string directory in Directory.GetDirectories(assemblyShadowRoot))
		{
			try
			{
				if (Directory.GetCreationTimeUtc(directory) < oldestAllowed)
				{
					Directory.Delete(directory, recursive: true);
				}
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
			{
			}
		}
	}
}
