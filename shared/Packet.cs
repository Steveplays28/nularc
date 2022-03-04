using System;
using System.IO;
using System.Security.Cryptography;

namespace NExLib
{
	/// <summary>
	/// Writeable/readable packet, dispose using <c>Dispose()</c> when the packet is no longer in use.
	/// </summary>
	public class Packet : IDisposable
	{
		public readonly byte packetNumber;
		public readonly byte connectedFunction;

		private readonly MemoryStream memoryStream;
		private readonly BinaryWriter binaryWriter;
		private readonly BinaryReader binaryReader;

		public Packet(byte packetNumber, byte connectedFunction)
		{
			memoryStream = new MemoryStream();
			binaryWriter = new BinaryWriter(memoryStream);
			binaryReader = new BinaryReader(memoryStream);

			this.packetNumber = packetNumber;
			this.connectedFunction = connectedFunction;

			// TODO: Error handling
		}
		public Packet(byte[] byteArray)
		{
			memoryStream = new MemoryStream();
			binaryWriter = new BinaryWriter(memoryStream);
			binaryReader = new BinaryReader(memoryStream);

			binaryWriter.Write(byteArray);
			memoryStream.Position = 0;

			packetNumber = ReadByte();
			connectedFunction = ReadByte();

			// TODO: Error handling
		}

		#region WriteData
		/// <summary>
		/// Prepends a header to the packet (containing the number of the packet, the connected function of the packet, the length of the packet's contents, and a checksum if enabled). <br/>
		/// Make sure to do this after all data has been written to the packet!
		/// </summary>
		public void WritePacketHeader()
		{
			// // Create new MemoryStream and BinaryWriter for the header
			// MemoryStream newMemoryStream = new();
			// BinaryWriter newBinaryWriter = new(newMemoryStream);

			// // Write header data to the new MemoryStream
			// newBinaryWriter.Write(packetNumber);
			// newBinaryWriter.Write(connectedFunction);

			// // Copy the old MemoryStream to the new MemoryStream, and update the memoryStream variable
			// memoryStream.Position = 0;
			// memoryStream.CopyTo(newMemoryStream);
			// memoryStream = newMemoryStream;

			// // Dispose the new MemoryStream and the new BinaryWriter
			// newBinaryWriter.Dispose();
			// newMemoryStream.Dispose();


			// Reset MemoryStream's position
			memoryStream.Position = 0;

			// Write header data to the MemoryStream
			binaryWriter.Write(packetNumber);
			binaryWriter.Write(connectedFunction);

			// Set MemoryStream's position back to the last byte
			memoryStream.Position = memoryStream.Length;
		}

		public void WriteData(bool data)
		{
			// Write data to packet
			binaryWriter.Write(data);
		}
		public void WriteData(int data)
		{
			// Write data to packet
			binaryWriter.Write(data);
		}
		public void WriteData(float data)
		{
			// Write data to packet
			binaryWriter.Write(data);
		}
		public void WriteData(string data)
		{
			// Write length prefix and data to packet
			binaryWriter.Write(data);
		}
		#endregion

		#region ReadData
		public bool ReadBoolean()
		{
			return binaryReader.ReadBoolean();
		}
		public byte ReadByte()
		{
			return binaryReader.ReadByte();
		}
		public int ReadInt32()
		{
			return binaryReader.ReadInt32();
		}
		public float ReadFloat()
		{
			return binaryReader.ReadSingle();
		}
		public string ReadString()
		{
			return binaryReader.ReadString();
		}
		#endregion

		/// <summmary>
		/// Calculates a SHA256 checksum from the binary writer's base stream.
		/// </summmary>
		// private string CalculateChecksum()
		// {
		// 	using (var sha256 = SHA256.Create())
		// 	{
		// 		byte[] checksum = sha256.ComputeHash(memoryStream);
		// 		return BitConverter.ToString(checksum).Replace("-", "").ToLowerInvariant();
		// 	}
		// }

		/// <summmary>
		/// Returns the packet's data as a byte array. <br/>
		/// Do not use if the packet is still being written to.
		/// </summmary>
		public byte[] ReturnData()
		{
			// Write all pending data to memory stream
			binaryWriter.Flush();

			// Return byte array
			return memoryStream.ToArray();
		}

		public void Dispose()
		{
			binaryWriter.Dispose();
			binaryReader.Dispose();
			memoryStream.Dispose();

			GC.SuppressFinalize(this);
		}
	}
}
