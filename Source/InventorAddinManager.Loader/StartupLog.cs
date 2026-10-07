using System.Globalization;
using System.IO;

namespace InventorAddinManager.Loader;

/// <summary>
/// 	Records a failure that occurs before the manager can write its own log.
/// </summary>
/// <remarks>
/// 	Inventor ignores an exception from <c>Activate</c>, so the manager does not appear and nothing tells you why.
/// 	This writes to the same daily file as the manager's log, so the manager deletes old files of both.
/// </remarks>
internal static class StartupLog
{
	/// <summary>
	/// 	Writes the failure and the exception to today's log file.
	/// </summary>
	/// <param name="stage">
	/// 	What the loader tried to do, ex. "loading the manager".
	/// </param>
	/// <param name="exception">
	/// 	The failure, written in full because this is its only record.
	/// </param>
	public static void WriteFailure(string stage, Exception exception)
	{
		string logDirectory = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"InventorAddinManager",
			"Logs");
		string filePath = Path.Combine(logDirectory, $"manager-{DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}.log");
		string text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)} Loader failed while {stage}.{Environment.NewLine}{exception}{Environment.NewLine}";

		try
		{
			_ = Directory.CreateDirectory(logDirectory);
			File.AppendAllText(filePath, text);
		}
		catch (Exception logException) when (logException is IOException or UnauthorizedAccessException)
		{
		}
	}
}
