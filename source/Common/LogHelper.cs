namespace NExLib.Common
{
	/// <summary>
	/// NExLib's helper class for logging.
	/// </summary>
	public class LogHelper
	{
		public enum LogLevel
		{
			Info,
			Warning,
			Error
		}
		public delegate void LogDelegate(LogLevel logLevel, string logMessage);
		public event LogDelegate? Log;
		public string Prefix;

		public LogHelper(string prefix)
		{
			Prefix = prefix;
		}

		public void LogMessage(LogLevel logLevel, string logMessage)
		{
			Log?.Invoke(logLevel, Prefix + logMessage);
		}
	}
}
