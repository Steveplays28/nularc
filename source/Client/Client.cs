using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NExLib.Common;

namespace NExLib.Client
{
	public class Client
	{
		public const string DefaultServerIp = "127.0.0.1";
		public const int DefaultServerPort = 24465;
		public const int MaxPacketsReceivedPerTick = 5;

		public UdpClient UdpClient = new();
		public string ServerIp { get; private set; } = DefaultServerIp;
		public int ServerPort { get; private set; } = DefaultServerPort;
		public bool IsConnected { get; private set; }

		public delegate void PacketCallback(Packet packet, IPEndPoint clientIPEndPoint);
		public event PacketCallback? PacketReceived;

		private IPEndPoint? serverEndPoint;
		private readonly LogHelper logHelper = new("[NExLib (Client)]: ");

		public void Close()
		{
			try
			{
				UdpClient?.Close();
				logHelper.LogMessage(LogHelper.LogLevel.Info, "Successfully closed the UdpClient!");
			}
			catch (SocketException e)
			{
				logHelper.LogMessage(LogHelper.LogLevel.Error, $"Failed closing the UdpClient: {e}");
			}
		}

		public void Tick()
		{
			ReceivePackets();
		}

		public void Connect(string ip, int port)
		{
			if (IsConnected)
			{
				logHelper.LogMessage(LogHelper.LogLevel.Warning, "Failed connecting to the server: already connected to a server.");
				return;
			}

			// Set server endpoint
			serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

			// Send connect packet
			using Packet packet = new((int)PacketMethod.Connect);
			SendPacket(packet);
			logHelper.LogMessage(LogHelper.LogLevel.Info, "Sent connect packet to the server.");
		}

		public void Disconnect()
		{
			if (!IsConnected)
			{
				logHelper.LogMessage(LogHelper.LogLevel.Warning, "Failed disconnecting from the server: not connected to a server.");
				return;
			}

			// Reset server endpoint
			serverEndPoint = null;

			// Send disconnect packet
			using Packet packet = new((int)PacketMethod.Disconnect);
			SendPacket(packet);
			logHelper.LogMessage(LogHelper.LogLevel.Info, "Sent disconnect packet to the server.");
		}

		/// <summary>
		/// Sends a packet to the server.
		/// </summary>
		/// <param name="packet">The packet to send.</param>
		/// <returns></returns>
		public void SendPacket(Packet packet)
		{
			// Write packet header
			packet.WritePacketHeader();

			// Get data from the packet
			byte[] packetData = packet.ReturnData();

			// Send the packet to the server
			UdpClient?.Send(packetData, packetData.Length, serverEndPoint);
		}

		/// <summary>
		/// Receives up to MaxPacketsReceivedPerTick asynchronously.
		/// </summary>
		private async void ReceivePackets()
		{
			for (int i = 0; i < MaxPacketsReceivedPerTick && UdpClient.Available > 0; i++)
			{
				// Extract data from the received packet
				UdpReceiveResult udpReceiveResult = await UdpClient.ReceiveAsync();
				IPEndPoint remoteEndPoint = udpReceiveResult.RemoteEndPoint;
				byte[] packetData = udpReceiveResult.Buffer;

				// Create new Packet object from the received packet data and invoke PacketReceived event
				using Packet packet = new(packetData);
				PacketReceived?.Invoke(packet, remoteEndPoint);
			}
		}
	}
}
