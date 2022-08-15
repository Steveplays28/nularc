using System;
using System.IO;

namespace NExLib.Common
{
	/// <summary>
	/// Writeable/readable packet, dispose using <c>Dispose()</c> when the packet is no longer in use.
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
		/// Creates a new Packet from a byte array, and sets the packet's header.
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

		/// <returns>All the data in the packet as a byte array.</returns>
		public byte[] ReturnData()
		{
			// Write all pending data to memory stream
			Writer.Flush();

			// Return data as byte array
			return memoryStream.ToArray();
		}

		/// <summary>
		/// Disposes of the Packet's BinaryWriter, BinaryReader, and MemoryStream.
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
