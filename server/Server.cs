using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NExLib
{
	public static class Server
	{
		#region Variables
		public struct UdpState
		{
			public IPEndPoint serverEndPoint;
			public UdpClient udpClient;
			// TODO: Implement packetCount
			public int packetCount;

			// Connected clients
			public Dictionary<IPEndPoint, int> connectedClientsIpToId;
			public Dictionary<int, IPEndPoint> connectedClientsIdToIp;

			// Saved clients
			public Dictionary<IPEndPoint, int> savedClientsIpToId;
			public Dictionary<int, IPEndPoint> savedClientsIdToIp;
		}
		public static UdpState udpState = new();

		private static readonly LogHelper _logHelper = new("[Server]: ");
		#endregion

		public static void Start(int port)
		{
			udpState.serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
			udpState.udpClient = new UdpClient(udpState.serverEndPoint);
			udpState.packetCount = 0;

			udpState.connectedClientsIpToId = new Dictionary<IPEndPoint, int>();
			udpState.connectedClientsIdToIp = new Dictionary<int, IPEndPoint>();
			udpState.savedClientsIpToId = new Dictionary<IPEndPoint, int>();
			udpState.savedClientsIdToIp = new Dictionary<int, IPEndPoint>();

			// Create and start a UDP receive thread for Server.ReceivePacket(), so it doesn't block Godot's main thread
			Thread udpReceiveThread = new(new ThreadStart(ReceivePacket))
			{
				Name = "UDP receive thread",
				IsBackground = true
			};
			udpReceiveThread.Start();
			// TODO: Server start try catch block

			_logHelper.LogInfo($"Server started on {udpState.serverEndPoint}.");
		}

		#region Sending packets
		/// <summary>
		/// Sends a packet to all clients.
		/// <br/>
		/// Important: the packet's recipientId should always be zero for the packet to be interpreted correctly on the clients.
		/// </summary>
		/// <param name="packet">The packet to send.</param>
		public static void SendPacketToAll(Packet packet)
		{
			// Write packet header
			packet.WritePacketHeader();

			// Get data from the packet
			byte[] packetData = packet.ReturnData();

			// Send the packet to all connected clients
			foreach (IPEndPoint connectedClient in udpState.connectedClientsIdToIp.Values)
			{
				udpState.udpClient.Send(packetData, packetData.Length, connectedClient);
			}
		}
		/// <summary>
		/// Sends a packet to a client.
		/// </summary>
		/// <param name="packet">The packet to send.</param>
		/// <param name="recipientId">The client that the packet should be sent to.</param>
		/// <returns></returns>
		public static void SendPacketTo(Packet packet, int recipientId)
		{
			// Write packet header
			packet.WritePacketHeader();

			// Get data from the packet
			byte[] packetData = packet.ReturnData();

			// Send the packet to the specified client
			if (udpState.connectedClientsIdToIp.TryGetValue(recipientId, out IPEndPoint? connectedClient))
			{
				udpState.udpClient.Send(packetData, packetData.Length, connectedClient);
			}
		}
		#endregion

		#region Receiving packets
		public static void ReceivePacket()
		{
			while (true)
			{
				// Extract data from the received packet
				IPEndPoint remoteEndPoint = new(IPAddress.Any, 0);
				byte[] packetData = udpState.udpClient.Receive(ref remoteEndPoint);

				// Construct new Packet object from the received packet
				using (Packet constructedPacket = new(packetData))
				{
					PacketCallbacksServer.PacketCallbacks[constructedPacket.connectedFunction].Invoke(constructedPacket);
				}

				Thread.Sleep(17);
			}
		}
		#endregion

		public static void OnConnect(Packet packet, IPEndPoint ipEndPoint)
		{
			// Accept the client's connection request
			int createdClientId = udpState.connectedClientsIdToIp.Count;
			udpState.connectedClientsIdToIp.Add(createdClientId, ipEndPoint);
			udpState.connectedClientsIpToId.Add(ipEndPoint, createdClientId);
			// TODO: Check if client isn't already connected

			string messageOfTheDay = "Hello, this is the message of the day! :)";

			// Send a new packet back to the newly connected client
			using (Packet newPacket = new(0, 0))
			{
				// Write the client ID to the packet
				newPacket.WriteData(createdClientId);

				// Write the message of the day to the packet
				newPacket.WriteData(messageOfTheDay);

				SendPacketTo(newPacket, udpState.connectedClientsIpToId[ipEndPoint]);
			}

			_logHelper.LogInfo($"New client connected from {ipEndPoint}.");
		}

		public static void Stop()
		{
			try
			{
				udpState.udpClient.Close();
				_logHelper.LogInfo($"Successfully closed the UdpClient!");
			}
			catch (SocketException e)
			{
				_logHelper.LogError($"Failed closing the UdpClient: {e}");
			}
		}
	}
}
