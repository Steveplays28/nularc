namespace SteveNetworking.Common
{
	/// <summary>
	/// SteveNetworking's helper class for logging.
	/// </summary>
	public class Logger
	{
		/// <summary>
		/// The log levels used by the logger.
		/// </summary>
		public enum LogLevel
		{
			/// <summary>
			/// Used for displaying info messages in the log.
			/// </summary>
			Info,
			/// <summary>
			/// Used for displaying warnings in the log.
			/// </summary>
			Warning,
			/// <summary>
			/// Used for displaying errors in the log.
			/// </summary>
			Error
		}
		/// <summary>
		/// Delegate used by the <see cref="Log"/> event.
		/// </summary>
		/// <param name="logLevel">The level of the log message.</param>
		/// <param name="logMessage">The message that will get logged.</param>
		public delegate void LogDelegate(LogLevel logLevel, string logMessage);
		/// <summary>
		/// Event that gets called when a message is logged by the library.
		/// </summary>
		public event LogDelegate Log;
		/// <summary>
		/// The text that will be prefixed to every log message.
		/// </summary>
		public string Prefix;

		/// <summary>
		/// Creates a new instance of the <see cref="Logger"/> which the library will use to log messages.
		/// </summary>
		/// <param name="prefix">The text that will be prefixed to every log message.</param>
		internal Logger(string prefix)
		{
			Prefix = prefix;
		}

		/// <summary>
		/// Logs a message.
		/// </summary>
		/// <param name="logLevel">The level of the log message.</param>
		/// <param name="logMessage">The message that will get logged.</param>
		internal void LogMessage(LogLevel logLevel, string logMessage)
		{
			Log?.Invoke(logLevel, Prefix + logMessage);
		}
	}
}
