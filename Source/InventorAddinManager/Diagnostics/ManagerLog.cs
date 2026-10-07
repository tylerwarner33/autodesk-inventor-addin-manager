using System.Globalization;
using System.IO;

namespace InventorAddinManager.Diagnostics;

/// <summary>
/// 	Writes one line for each event to a daily log file.
/// </summary>
/// <remarks>
/// 	The add-in runs inside Inventor with no console, so this file is the only record of a click that does nothing.
/// 	A failure to write is ignored: the log must never stop a command.
/// </remarks>
internal static class ManagerLog
{
	private static readonly string _logDirectory = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		nameof(InventorAddinManager),
		"Logs");

	private static readonly TimeSpan _maximumAge = TimeSpan.FromDays(7);

	private static readonly object _gate = new();

	/// <summary>
	/// 	Gets the folder that holds the log files.
	/// </summary>
	public static string LogDirectory => _logDirectory;

	/// <summary>
	/// 	Writes a line to today's log file.
	/// </summary>
	public static void Write(string message)
	{
		string line = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)} {message}{Environment.NewLine}";
		string filePath = Path.Combine(_logDirectory, $"manager-{DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}.log");

		try
		{
			lock (_gate)
			{
				Directory.CreateDirectory(_logDirectory);
				File.AppendAllText(filePath, line);
			}
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
		}
	}

	/// <summary>
	/// 	Writes a line and the exception to today's log file.
	/// </summary>
	public static void Write(string message, Exception exception)
	{
		Write($"{message}{Environment.NewLine}{exception}");
	}

	/// <summary>
	/// 	Deletes log files older than a week.
	/// </summary>
	public static void DeleteOldFiles()
	{
		try
		{
			if (Directory.Exists(_logDirectory) is false)
			{
				return;
			}

			DateTime oldestAllowed = DateTime.Now - _maximumAge;
			foreach (string file in Directory.GetFiles(_logDirectory, "manager-*.log"))
			{
				if (File.GetLastWriteTime(file) < oldestAllowed)
				{
					File.Delete(file);
				}
			}
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
		}
	}
}
