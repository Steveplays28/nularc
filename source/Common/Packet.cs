using System;
using System.IO;

namespace Nularc.Common
{
	/// <summary>
	/// Writeable/readable packet.
	/// </summary>
	public class Packet : IDisposable
	{
		/// <summary>
		/// The header's length in bytes.
		/// </summary>
		public const int HeaderLength = 4;
		/// <summary>
		/// Used for writing to a packet.
		/// </summary>
		public BinaryWriter Writer;
		/// <summary>
		/// Used for reading a packet.
		/// </summary>
		public BinaryReader Reader;
		/// <summary>
		/// The packet's type, used to identify the packet.
		/// </summary>
		public readonly int Type;

		private readonly MemoryStream memoryStream = new();

		/// <summary>
		/// Creates a new empty packet, containing only the header.
		/// </summary>
		/// <param name="type">The packet's type.</param>
		public Packet(int type)
		{
			Writer = new BinaryWriter(memoryStream);
			Reader = new BinaryReader(memoryStream);

			Type = type;

			Writer.Write(type);
		}
		/// <summary>
		/// Creates a new packet from a byte array containing a valid header and data.
		/// </summary>
		/// <param name="byteArray">The byte array to create the packet from.</param>
		/// <exception cref="InvalidPacketHeaderException" />
		/// <exception cref="EndOfStreamException" />
		public Packet(byte[] byteArray)
		{
			if (byteArray.Length <= 0)
			{
				throw new InvalidPacketHeaderException();
			}

			Writer = new BinaryWriter(memoryStream);
			Reader = new BinaryReader(memoryStream);

			Writer.Write(byteArray);
			memoryStream.Position = 0;

			Type = Reader.ReadInt32();
		}

		/// <summary>
		/// Gets the packet's header and data.
		/// </summary>
		/// <returns>A byte array containing the packet's header and data.</returns>
		public byte[] ReturnData()
		{
			// Write all pending data to memory stream
			Writer.Flush();

			// Return data as byte array
			return memoryStream.ToArray();
		}

		/// <summary>
		/// Disposes of the packet's underlying writer, reader, and memory stream objects.
		/// </summary>
		public void Dispose()
		{
			Writer.Dispose();
			Reader.Dispose();
			memoryStream.Dispose();

			GC.SuppressFinalize(this);
		}
	}
}
