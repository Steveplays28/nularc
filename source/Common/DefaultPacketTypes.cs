namespace SteveNetworking.Common
{
	/// <summary>
	/// The default built-in packet types, used by the library.
	/// The enum values start at -1, and count down, thus not conflicting with user defined packet types.
	/// </summary>
	public enum DefaultPacketTypes
	{
		/// <summary>
		/// Used for sending connect request/confirmation packets.
		/// </summary>
		Connect = -1,
		/// <summary>
		/// Used for sending disconnect request/confirmation packets.
		/// </summary>
		Disconnect = -2
	}
}
