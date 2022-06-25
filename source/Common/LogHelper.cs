namespace NExLib.Common
{
	/// <summary>
	/// NExLib's helper class for logging.
	/// </summary>
	public class LogHelper
	{
		public enum Loglevel
		{
			Info,
			Warning,
			Error
		}
		public delegate void LogDelegate(Loglevel logLevel, string logMessage);
		public event LogDelegate? Log;
		public string Prefix;

		public LogHelper(string prefix)
		{
			Prefix = prefix;
		}

		public void LogMessage(Loglevel logLevel, string logMessage)
		{
			Log?.Invoke(logLevel, Prefix + logMessage);
		}
	}
}
