using System;
using System.IO;

namespace NExLib.Common
{
	/// <summary>
	/// Writeable/readable packet, dispose using <c>Dispose()</c> when the packet is no longer in use.
	/// </summary>
	public class Packet : IDisposable
	{
		public readonly int ConnectedMethod;

		public BinaryWriter Writer;
		public BinaryReader Reader;

		private readonly MemoryStream memoryStream = new MemoryStream();

		/// <summary>
		/// Creates a new empty Packet, containing only the ConnectedMethod property.
		/// </summary>
		/// <param name="connectedMethod">The method that is connected to the Packet.</param>
		public Packet(int connectedMethod)
		{
			Writer = new BinaryWriter(memoryStream);
			Reader = new BinaryReader(memoryStream);

			ConnectedMethod = connectedMethod;
		}
		/// <summary>
		/// Creates a new Packet from a byte array, and sets the Packet's ConnectedMethod property to the first byte of given byte array.
		/// </summary>
		/// <param name="byteArray">The byte array to create the Packet from.</param>
		public Packet(byte[] byteArray)
		{
			Writer = new BinaryWriter(memoryStream);
			Reader = new BinaryReader(memoryStream);

			Writer.Write(byteArray);
			memoryStream.Position = 0;

			ConnectedMethod = Reader.ReadInt32();
		}

		/// <summary>
		/// Prefixes a header to the Packet, containing the connected method of the Packet. <br/>
		/// </summary>
		public void WritePacketHeader()
		{
			// Reset MemoryStream's position
			memoryStream.Position = 0;

			// Write header data to the Packet
			Writer.Write(ConnectedMethod);

			// Set MemoryStream's position back to the last byte
			memoryStream.Position = memoryStream.Length;
		}

		/// <returns>All the data in the Packet as a byte array.</returns>
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
