using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NExLib.Common;

namespace NExLib.Server
{
	public class Server
	{
		public const int MaxPacketsReceivedPerTick = 5;

		public UdpClient UdpClient;
		public Dictionary<IPEndPoint, int> ConnectedClientsIpToId = new();
		public Dictionary<int, IPEndPoint> ConnectedClientsIdToIp = new();
		public Dictionary<IPEndPoint, int> SavedClientsIpToId = new();
		public Dictionary<int, IPEndPoint> SavedClientsIdToIp = new();

		public delegate void PacketCallback(Packet packet, IPEndPoint clientIPEndPoint);
		public event PacketCallback? PacketReceived;


		private int packetCount = 0;
		private readonly LogHelper logHelper;
		private readonly IPEndPoint serverEndPoint;

		public Server(int port)
		{
			serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
			UdpClient = new UdpClient(serverEndPoint);

			logHelper = new LogHelper("[NExLib (Server)]: ");
		}

		public void Start()
		{
			PacketReceived += OnConnect;

			logHelper.LogMessage(LogHelper.Loglevel.Info, $"Server started on {serverEndPoint}.");
		}

		public void Stop()
		{
			try
			{
				UdpClient.Close();
				logHelper.LogMessage(LogHelper.Loglevel.Info, $"Successfully closed the UdpClient!");
			}
			catch (SocketException e)
			{
				logHelper.LogMessage(LogHelper.Loglevel.Info, $"Failed closing the UdpClient: {e}");
			}
		}

		public void Tick()
		{
			ReceivePacket();
		}

		/// <summary>
		/// Sends a packet to all clients.
		/// </summary>
		/// <param name="packet">The packet to send.</param>
		public void SendPacketToAll(Packet packet)
		{
			// Write packet header
			packet.WritePacketHeader();

			// Get data from the packet
			byte[] packetData = packet.ReturnData();

			// Send the packet to all connected clients
			foreach (IPEndPoint connectedClient in ConnectedClientsIdToIp.Values)
			{
				UdpClient.Send(packetData, packetData.Length, connectedClient);
			}
		}
		/// <summary>
		/// Sends a packet to a client.
		/// </summary>
		/// <param name="packet">The packet to send.</param>
		/// <param name="recipientId">The client that the packet should be sent to.</param>
		/// <returns></returns>
		public void SendPacketTo(Packet packet, int recipientId)
		{
			// Write packet header
			packet.WritePacketHeader();

			// Get data from the packet
			byte[] packetData = packet.ReturnData();

			// Send the packet to the specified client
			if (ConnectedClientsIdToIp.TryGetValue(recipientId, out IPEndPoint? connectedClient))
			{
				UdpClient.Send(packetData, packetData.Length, connectedClient);
			}
		}

		public void OnConnect(Packet packet, IPEndPoint clientIPEndPoint)
		{
			// Accept the client's connection request
			int clientId = ConnectedClientsIdToIp.Count;
			ConnectedClientsIdToIp.Add(clientId, clientIPEndPoint);
			ConnectedClientsIpToId.Add(clientIPEndPoint, clientId);
			// TODO: Check if client isn't already connected

			string messageOfTheDay = "Hello, this is the message of the day! :)";

			// Send a new packet back to the newly connected client
			using (Packet newPacket = new(0, 0))
			{
				// Write the client ID to the packet
				newPacket.WriteData(clientId);

				// Write the message of the day to the packet
				newPacket.WriteData(messageOfTheDay);

				SendPacketTo(newPacket, clientId);
			}

			logHelper.LogMessage(LogHelper.Loglevel.Info, $"New client connected from {clientIPEndPoint}.");
		}

		private async void ReceivePacket()
		{
			for (int i = 0; i < MaxPacketsReceivedPerTick && UdpClient.Available > 0; i++)
			{
				// Extract data from the received packet
				UdpReceiveResult udpReceiveResult = await UdpClient.ReceiveAsync();
				IPEndPoint remoteEndPoint = udpReceiveResult.RemoteEndPoint;
				byte[] packetData = udpReceiveResult.Buffer;

				// Construct new Packet object from the received packet
				using Packet constructedPacket = new(packetData);
				PacketReceived?.Invoke(constructedPacket, remoteEndPoint);
			}
		}
	}
}
