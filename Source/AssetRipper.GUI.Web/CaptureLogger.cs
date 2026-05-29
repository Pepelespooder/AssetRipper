using AssetRipper.Import.Logging;

namespace AssetRipper.GUI.Web;

internal sealed class CaptureLogger : ILogger
{
	private readonly List<string> _log;

	public CaptureLogger(List<string> log) => _log = log;

	public void Log(LogType type, LogCategory category, string message)
	{
		if (type is LogType.Verbose or LogType.Debug)
		{
			return;
		}
		lock (_log) { _log.Add(message); }
	}

	public void BlankLine(int numLines) { }
}
