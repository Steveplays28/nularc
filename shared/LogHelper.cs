#if GODOT
using Godot;
#elif NETFRAMEWORK
using System;
#endif

namespace NExLib
{
	/// <summary>
	/// NExLib's helper class for logging. Supports a prefix, and logging with .NET &amp; Godot.
	/// </summary>
	public class LogHelper
	{
		public LogHelper(string prefix)
		{
			Prefix = prefix;
		}

		public string Prefix;

		#region LogInfo
		/// <summary>
		/// Logs an info message to the console, optionally overriding the LogHelper instance prefix.
		/// </summary>
		/// <param name="message">The message that will be logged to the console.</param>
		public void LogInfo(string message)
		{
			string logOutput = string.Concat(Prefix, message);

#if GODOT
			GD.Print(logOutput);
#elif NETFRAMEWORK
		Console.WriteLine(logOutput);
#endif
		}

		/// <summary>
		/// Logs an info message to the console, optionally overriding the LogHelper instance prefix.
		/// </summary>
		/// <param name="message">The message that will be logged to the console.</param>
		/// <param name="prefixOverride">The prefix to override the LogHelper instance prefix with.</param>
		public static void LogInfo(string message, string prefixOverride)
		{
			string logOutput = string.Concat(prefixOverride, message);

#if GODOT
			GD.Print(logOutput);
#elif NETFRAMEWORK
		Console.WriteLine(logOutput);
#endif
		}
		#endregion

		#region LogWarning
		/// <summary>
		/// Logs a warning message to the console, optionally overriding the LogHelper instance prefix.
		/// </summary>
		/// <param name="message">The message that will be logged to the console.</param>
		public void LogWarning(string message)
		{
			string logOutput = string.Concat(Prefix, message);

#if GODOT
			GD.PushWarning(logOutput);
#elif NETFRAMEWORK
		Console.WriteLine($"[WARN] {logOutput}");
#endif
		}

		/// <summary>
		/// Logs a warning message to the console, optionally overriding the LogHelper instance prefix.
		/// </summary>
		/// <param name="message">The message that will be logged to the console.</param>
		public static void LogWarning(string message, string prefixOverride)
		{
			string logOutput = string.Concat(prefixOverride, message);

#if GODOT
			GD.PushWarning(logOutput);
#elif NETFRAMEWORK
		Console.WriteLine($"[WARN] {logOutput}");
#endif
		}
		#endregion

		#region LogError
		/// <summary>
		/// Logs an error message to the console, optionally overriding the LogHelper instance prefix.
		/// </summary>
		/// <param name="message">The message that will be logged to the console.</param>
		public void LogError(string message)
		{
			string logOutput = string.Concat(Prefix, message);

#if GODOT
			GD.PushError(logOutput);
#elif NETFRAMEWORK
		Console.Error.WriteLine(logOutput);
#endif
		}

		/// <summary>
		/// Logs an error message to the console, optionally overriding the LogHelper instance prefix.
		/// </summary>
		/// <param name="message">The message that will be logged to the console.</param>
		public static void LogError(string message, string prefixOverride)
		{
			string logOutput = string.Concat(prefixOverride, message);

#if GODOT
			GD.PushError(logOutput);
#elif NETFRAMEWORK
		Console.Error.WriteLine(logOutput);
#endif
		}
		#endregion
	}
}
