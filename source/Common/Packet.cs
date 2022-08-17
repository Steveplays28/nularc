using System;
using System.IO;

namespace NExLib.Common
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
		public BinaryWriter Writer;
		public BinaryReader Reader;
		public readonly int Type;

		private readonly MemoryStream memoryStream = new MemoryStream();

		/// <summary>
		/// Creates a new empty packet, containing only the header.
		/// </summary>
		/// <param name="Type">The packet's type.</param>
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
		public Packet(byte[] byteArray)
		{
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
