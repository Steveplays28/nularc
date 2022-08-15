using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NExLib.Common;

namespace NExLib.Server
{
	public class Server
	{
		public int MaxPacketsReceivedPerTick = 5;
		public delegate void PacketReceivedEventHandler(Packet packet, IPEndPoint IPEndPoint);
		public event PacketReceivedEventHandler PacketReceived;
		public event PacketReceivedEventHandler Connected;
		public event PacketReceivedEventHandler Disconnected;
		public UdpClient UdpClient { get; private set; }
		public IPEndPoint IPEndPoint { get; private set; }
		public bool HasStarted { get; private set; }
		public bool IsStopping { get; private set; }
		public Dictionary<IPEndPoint, int> ConnectedClientsIPToID { get; private set; } = new Dictionary<IPEndPoint, int>();
		public Dictionary<int, IPEndPoint> ConnectedClientsIDToIP { get; private set; } = new Dictionary<int, IPEndPoint>();
		public Dictionary<IPEndPoint, int> SavedClientsIpToId { get; private set; } = new Dictionary<IPEndPoint, int>();
		public Dictionary<int, IPEndPoint> SavedClientsIdToIp { get; private set; } = new Dictionary<int, IPEndPoint>();
		public readonly LogHelper LogHelper = new LogHelper("[NExLib (Server)]: ");

		private readonly Dictionary<int, List<PacketReceivedEventHandler>> PacketListeners = new Dictionary<int, List<PacketReceivedEventHandler>>();

		public Server()
		{
			PacketReceived += OnPacketReceived;

			Listen((int)DefaultPacketTypes.Connect, OnConnected);
			Listen((int)DefaultPacketTypes.Connect, OnDisconnected);
		}

		public void Start(int port)
		{
			UdpClient = new UdpClient(port);
			IPEndPoint = UdpClient.Client.LocalEndPoint as IPEndPoint;

			HasStarted = true;
			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Server started on {IPEndPoint}.");
		}

		public void Stop()
		{
			try
			{
				if (IsStopping)
				{
					LogHelper.LogMessage(LogHelper.LogLevel.Warning, $"Server.Stop() was called, but the server is already trying to stop!");
					return;
				}

				IsStopping = true;
				UdpClient.Close();
				IPEndPoint = null;
				ConnectedClientsIDToIP = new Dictionary<int, IPEndPoint>();
				ConnectedClientsIPToID = new Dictionary<IPEndPoint, int>();

				HasStarted = false;
				IsStopping = false;
				LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Successfully stopped the server!");
			}
			catch (SocketException e)
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Failed stopping the server: {e}");
			}
		}

		public void Tick()
		{
			ReceivePackets();
		}

		/// <summary>
		/// Sends a packet to all clients.
		/// </summary>
		/// <param name="packet">The packet to send.</param>
		public void SendPacketToAll(Packet packet)
		{
			// Get data from the packet
			byte[] packetData = packet.ReturnData();

			// Send the packet to all connected clients
			foreach (IPEndPoint connectedClient in ConnectedClientsIDToIP.Values)
			{
				UdpClient.Send(packetData, packetData.Length, connectedClient);
			}
		}

		public void Listen(int packetType, PacketReceivedEventHandler method)
		{
			if (!PacketListeners.ContainsKey(packetType))
			{
				var packetListeners = new List<PacketReceivedEventHandler>
				{
					method
				};

				PacketListeners.Add(packetType, packetListeners);
				return;
			}

			PacketListeners[packetType].Add(method);
		}

		/// <summary>
		/// Sends a packet to a client.
		/// </summary>
		/// <param name="packet">The packet to send.</param>
		/// <param name="clientId">The client that the packet should be sent to.</param>
		/// <returns></returns>
		public void SendPacket(Packet packet, int clientId)
		{
			// Get data from the packet
			byte[] packetData = packet.ReturnData();

			// Send the packet to the specified client
			if (ConnectedClientsIDToIP.TryGetValue(clientId, out IPEndPoint connectedClient))
			{
				UdpClient.Send(packetData, packetData.Length, connectedClient);
			}
		}

		/// <summary>
		/// Receives up to MaxPacketsReceivedPerTick asynchronously.
		/// </summary>
		private async void ReceivePackets()
		{
			if (UdpClient == null)
			{
				return;
			}

			for (int i = 0; i < MaxPacketsReceivedPerTick && UdpClient.Available > 0; i++)
			{
				try
				{
					// Extract data from the received packet
					UdpReceiveResult udpReceiveResult = await UdpClient.ReceiveAsync();
					IPEndPoint remoteIPEndPoint = udpReceiveResult.RemoteEndPoint;
					byte[] packetData = udpReceiveResult.Buffer;

					// Create new packet object from the received packet data
					using (Packet packet = new Packet(packetData))
					{
						// Check if packet contains header and data
						if (packetData.Length <= 0)
						{
							LogHelper.LogMessage(LogHelper.LogLevel.Warning, $"Received an empty packet of type {packet.Type} (header and data missing).");
						}
						else if (packetData.Length < Packet.HeaderLength)
						{
							LogHelper.LogMessage(LogHelper.LogLevel.Warning, $"Received an empty packet of type {packet.Type} (header incomplete and data missing).");
						}
						else if (packetData.Length == Packet.HeaderLength)
						{
							LogHelper.LogMessage(LogHelper.LogLevel.Warning, $"Received an empty packet of type {packet.Type} (data missing).");
						}

						// Invoke packet received event
						if (PacketReceived != null)
						{
							PacketReceived.Invoke(packet, remoteIPEndPoint);
						}
					}
				}
				catch (Exception e)
				{
					LogHelper.LogMessage(LogHelper.LogLevel.Error, $"Error occurred while trying to receive packet from server: {e}");
				}
			}
		}

		private void OnPacketReceived(Packet packet, IPEndPoint IPEndPoint)
		{
			foreach (PacketReceivedEventHandler packetReceivedEventHandler in PacketListeners[packet.Type])
			{
				packetReceivedEventHandler.Invoke(packet, IPEndPoint);
			}
		}

		private void OnConnected(Packet packet, IPEndPoint IPEndPoint)
		{
			// Check if client is already connected
			if (ConnectedClientsIDToIP.ContainsValue(IPEndPoint))
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Warning, $"Client with IP {IPEndPoint} tried to connect, but is already connected!");
				return;
			}

			// Accept the client's connection request
			int clientId = ConnectedClientsIDToIP.Count;
			ConnectedClientsIDToIP.Add(clientId, IPEndPoint);
			ConnectedClientsIPToID.Add(IPEndPoint, clientId);

			// Send a packet back to the client
			using (Packet newPacket = new Packet((int)DefaultPacketTypes.Connect))
			{
				// Write the client ID to the packet
				newPacket.Writer.Write(clientId);

				SendPacket(newPacket, clientId);
			}

			if (Connected != null)
			{
				Connected.Invoke(packet, IPEndPoint);
			}
			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"New client connected from {IPEndPoint}");
		}

		private void OnDisconnected(Packet packet, IPEndPoint IPEndPoint)
		{
			// Check if client is already disconnected
			if (!ConnectedClientsIDToIP.ContainsValue(IPEndPoint))
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Warning, $"Client with IP {IPEndPoint} tried to disconnect, but has already disconnected!");
				return;
			}

			// Disconnect the client
			ConnectedClientsIDToIP.Remove(ConnectedClientsIPToID[IPEndPoint]);
			ConnectedClientsIPToID.Remove(IPEndPoint);

			if (Disconnected != null)
			{
				Disconnected.Invoke(packet, IPEndPoint);
			}
			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Client {IPEndPoint} disconnected.");
		}
	}
}
