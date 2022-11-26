using System;

namespace Nularc.Common;

/// <summary>
/// The exception that is thrown when a packet has a header that is in an incorrect format, corrupted, or empty.
/// </summary>
[Serializable]
public class InvalidPacketHeaderException : Exception
{
	/// <summary>
	///  Initializes a new instance of the <see cref="InvalidPacketHeaderException"/> class.
	/// </summary>
	public InvalidPacketHeaderException()
	{ }

	/// <summary>
	/// Initializes a new instance of the <see cref="InvalidPacketHeaderException"/> class with a specified error message.
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	public InvalidPacketHeaderException(string message)
		: base(message)
	{ }

	/// <summary>
	/// Initializes a new instance of the <see cref="InvalidPacketHeaderException"/> class with a reference to the inner exception that is the cause of this exception.
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerException">The exception that is the cause of the current exception. If the innerException parameter is not null, the current exception is raised in a catch block that handles the inner exception.</param>
	public InvalidPacketHeaderException(string message, Exception innerException)
		: base(message, innerException)
	{ }
}
