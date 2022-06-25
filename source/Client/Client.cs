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

		public UdpClient? UdpClient;
		public string ServerIp { get; private set; } = DefaultServerIp;
		public int ServerPort { get; private set; } = DefaultServerPort;
		public int PacketCount { get; private set; } = 0;
		public bool IsConnected { get; private set; }

		public delegate void PacketCallback(Packet packet);
		public event PacketCallback? PacketReceived;

		private IPEndPoint? serverEndPoint;
		private readonly IPEndPoint? localEndPoint;
		private readonly LogHelper logHelper = new("[NExLib (Client)]: ");

		public Client()
		{
			UdpClient = new UdpClient();
			localEndPoint = UdpClient.Client.LocalEndPoint as IPEndPoint;

			try
			{
				// Create and start a background thread for ReceivePacket(), so it doesn't block Godot's main thread
				Thread udpReceiveThread = new(new ThreadStart(ReceivePacket))
				{
					Name = "UDP receive thread",
					IsBackground = true
				};
				udpReceiveThread.Start();
				logHelper.LogMessage(LogHelper.Loglevel.Info, $"Started listening for messages from the server on {localEndPoint}.");
			}
			catch (Exception e)
			{
				logHelper.LogMessage(LogHelper.Loglevel.Error, $"Failed initializing the UdpClient: couldn't create background thread \"UDP receive thread\".\n{e}");
			}
		}

		public void Close()
		{
			try
			{
				UdpClient?.Close();
				logHelper.LogMessage(LogHelper.Loglevel.Info, "Successfully closed the UdpClient!");
			}
			catch (SocketException e)
			{
				logHelper.LogMessage(LogHelper.Loglevel.Error, $"Failed closing the UdpClient: {e}");
			}
		}

		public void Connect(string ip, int port)
		{
			if (IsConnected)
			{
				logHelper.LogMessage(LogHelper.Loglevel.Warning, "Failed connecting to the server: already connected to a server.");
				return;
			}

			// Set server endpoint
			serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

			// Send connect packet
			using Packet packet = new(0, 0);
			SendPacket(packet);
			logHelper.LogMessage(LogHelper.Loglevel.Info, "Sent connect packet to the server.");
		}

		public void Disconnect()
		{
			if (!IsConnected)
			{
				logHelper.LogMessage(LogHelper.Loglevel.Warning, "Failed disconnecting from the server: not connected to a server.");
				return;
			}

			// Reset server endpoint
			serverEndPoint = null;

			// Send disconnect packet
			using Packet packet = new(0, 0);
			SendPacket(packet);
			logHelper.LogMessage(LogHelper.Loglevel.Info, "Sent disconnect packet to the server.");
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

		private void ReceivePacket()
		{
			try
			{
				// Extract data from the received packet
				IPEndPoint remoteEndPoint = new(IPAddress.Parse(DefaultServerIp), DefaultServerPort);
				byte[]? packetData = UdpClient?.Receive(ref remoteEndPoint);

				// Debug
				// logHelper.LogMessage(LogHelper.Loglevel.Info, $"Received bytes: {string.Join(", ", packetData)}");

				// Construct new Packet object from the received packet
				if (packetData != null)
				{
					using Packet constructedPacket = new(packetData);
					PacketReceived?.Invoke(constructedPacket);
				}
			}
			catch (Exception e)
			{
				logHelper.LogMessage(LogHelper.Loglevel.Error, $"Failed receiving a packet from the server: {e}\nCheck if the client is connected properly!");
			}
		}
	}
}
