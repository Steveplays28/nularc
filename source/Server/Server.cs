using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using SteveNetworking.Common;

namespace SteveNetworking.Server
{
	/// <summary>
	/// UDP server which handles (multiple) connections to clients.
	/// </summary>
	public class Server
	{
		/// <summary>
		/// Event handler for when packets are received and processed.
		/// </summary>
		/// <param name="packet">The packet that was received.</param>
		/// <param name="ipEndPoint">The IP endpoint of the client the packet was received from.</param>
		/// <param name="clientID">The ID of the client the packet was received from.</param>
		public delegate void PacketReceivedEventHandler(Packet packet, IPEndPoint ipEndPoint, int clientID);
		/// <summary>
		/// Event handler for when a client has successfully (dis)connected.
		/// </summary>
		/// <param name="ipEndPoint">The IP endpoint of the client that has successfully (dis)connected.</param>
		/// <param name="clientID">The ID of the client that has successfully (dis)connected.</param>
		public delegate void ConnectedEventHandler(IPEndPoint ipEndPoint, int clientID);
		/// <summary>
		/// Event that gets called when a client has successfully connected.
		/// </summary>
		public event ConnectedEventHandler ClientConnected;
		/// <summary>
		/// Event that gets called when a client has successfully disconnected.
		/// </summary>
		public event ConnectedEventHandler ClientDisconnected;
		/// <summary>
		/// The base UDP client class that is built on top of.
		/// </summary>
		public UdpClient UdpClient { get; private set; }
		/// <summary>
		/// The IP endpoint of the local server. <see langword="null"/> when the server isn't started.
		/// </summary>
		public IPEndPoint IPEndPoint { get; private set; }
		/// <summary>
		/// If the server has started. Some variables are <see langword="null"/> if the server isn't started, see the variables' documentation.
		/// </summary>
		public bool HasStarted { get; private set; }
		/// <summary>
		/// If the server is stopping. Some variables are <see langword="null"/> if the server isn't connected to a server, see the variables' documentation.
		/// </summary>
		public bool IsStopping { get; private set; }
		/// <summary>
		/// Maximum amount of packets that are processed per tick.
		/// </summary>
		public int MaxPacketsReceivedPerTick = 5;
		/// <summary>
		/// A dictionary containing all the connected clients, mapped as IP->ID.
		/// </summary>
		public Dictionary<IPEndPoint, int> ConnectedClientsIPToID { get; private set; } = new Dictionary<IPEndPoint, int>();
		/// <summary>
		/// A dictionary containing all the connected clients, mapped as ID->IP.
		/// </summary>
		public Dictionary<int, IPEndPoint> ConnectedClientsIDToIP { get; private set; } = new Dictionary<int, IPEndPoint>();
		/// <summary>
		/// The server's logger.
		/// </summary>
		public readonly Logger Logger = new("[SteveNetworking (Server)]: ");

		/// <summary>
		/// Event that is called when a packet gets received and processed.
		/// </summary>
		private event PacketReceivedEventHandler PacketReceived;
		private readonly Dictionary<int, List<PacketReceivedEventHandler>> PacketListeners = new();

		/// <summary>
		/// Initialises the server.
		/// </summary>
		public Server()
		{
			PacketReceived += OnPacketReceived;

			Listen((int)DefaultPacketTypes.Disconnect, OnDisconnect);
		}

		/// <summary>
		/// Starts a new instance of the server on the specified port.
		/// </summary>
		/// <param name="port">The port to use for the server.</param>
		public void Start(int port)
		{
			UdpClient = new UdpClient(port);
			IPEndPoint = (IPEndPoint)UdpClient.Client.LocalEndPoint;

			HasStarted = true;
			Logger.LogMessage(Logger.LogLevel.Info, $"Server started successfully on {IPEndPoint}.");
		}

		/// <summary>
		/// Stops the server instance.
		/// </summary>
		public void Stop()
		{
			try
			{
				if (IsStopping)
				{
					Logger.LogMessage(Logger.LogLevel.Warning, $"Failed stopping the server: the server is already trying to stop.");
					return;
				}

				IsStopping = true;
				UdpClient.Close();
				IPEndPoint = null;
				ConnectedClientsIDToIP = new();
				ConnectedClientsIPToID = new();

				HasStarted = false;
				IsStopping = false;
				Logger.LogMessage(Logger.LogLevel.Info, $"Server stopped successfully.");
			}
			catch (SocketException e)
			{
				Logger.LogMessage(Logger.LogLevel.Error, $"Failed stopping the server: {e}");
			}
		}

		/// <summary>
		/// Should be ran every frame, put this in your app's main loop.
		/// </summary>
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

		/// <summary>
		/// Listens for packets of certain types getting received, and notifies subscribed methods.
		/// </summary>
		/// <param name="packetType">The packet type to listen for.</param>
		/// <param name="method">The method to subscribe with.</param>
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
		/// <param name="clientID">The client that the packet should be sent to.</param>
		/// <returns></returns>
		public void SendPacket(Packet packet, int clientID)
		{
			// Get data from the packet
			byte[] packetData = packet.ReturnData();

			// Send the packet to the specified client
			if (ConnectedClientsIDToIP.TryGetValue(clientID, out IPEndPoint connectedClient))
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
					using Packet packet = new(packetData);

					// Check if the packet is a user defined packet, and if so, contains a header and data
					if (packet.Type >= 0)
					{
						if (packetData.Length <= 0)
						{
							Logger.LogMessage(Logger.LogLevel.Warning, $"Received an empty packet of type {packet.Type} (header and data missing).");
						}
						else if (packetData.Length < Packet.HeaderLength)
						{
							Logger.LogMessage(Logger.LogLevel.Warning, $"Received an empty packet of type {packet.Type} (header incomplete and data missing).");
						}
						else if (packetData.Length == Packet.HeaderLength)
						{
							Logger.LogMessage(Logger.LogLevel.Warning, $"Received an empty packet of type {packet.Type} (data missing).");
						}
					}

					// Check if the client that the packet was sent from is connected to the server
					if (packet.Type == (int)DefaultPacketTypes.Connect && !ConnectedClientsIPToID.ContainsKey(remoteIPEndPoint))
					{
						// Handle connection request
						OnClientConnect(remoteIPEndPoint);
					}
					else if (packet.Type != (int)DefaultPacketTypes.Connect && !ConnectedClientsIPToID.ContainsKey(remoteIPEndPoint))
					{
						Logger.LogMessage(Logger.LogLevel.Warning, $"Client {remoteIPEndPoint} tried to send a packet, but isn't connected to the server.");
					}
					else
					{
						// Invoke packet received event
						int clientID = ConnectedClientsIPToID[remoteIPEndPoint];
						PacketReceived?.Invoke(packet, remoteIPEndPoint, clientID);
					}
				}
				catch (Exception e)
				{
					Logger.LogMessage(Logger.LogLevel.Error, $"Failed receiving a packet from a client: {e}");
				}
			}
		}

		private void OnPacketReceived(Packet packet, IPEndPoint ipEndPoint, int clientID)
		{
			if (PacketListeners.ContainsKey(packet.Type))
			{
				foreach (PacketReceivedEventHandler packetReceivedEventHandler in PacketListeners[packet.Type])
				{
					packetReceivedEventHandler.Invoke(packet, ipEndPoint, clientID);
				}
			}
		}

		private void OnClientConnect(IPEndPoint ipEndPoint)
		{
			// Check if client is already connected
			if (ConnectedClientsIDToIP.ContainsValue(ipEndPoint))
			{
				int alreadyConnectedClientID = ConnectedClientsIPToID[ipEndPoint];
				Logger.LogMessage(Logger.LogLevel.Warning, $"Client {alreadyConnectedClientID} ({ipEndPoint}) failed to connect: already connected.");
				return;
			}

			// Accept the client's connection request
			int clientID = ConnectedClientsIDToIP.Count;
			ConnectedClientsIDToIP.Add(clientID, ipEndPoint);
			ConnectedClientsIPToID.Add(ipEndPoint, clientID);

			// Send a packet back to the client
			using (Packet newPacket = new((int)DefaultPacketTypes.Connect))
			{
				// Write the client ID to the packet
				newPacket.Writer.Write(clientID);

				SendPacket(newPacket, clientID);
			}

			ClientConnected.Invoke(ipEndPoint, clientID);
			Logger.LogMessage(Logger.LogLevel.Info, $"Client {clientID} ({IPEndPoint}) successfully connected.");
		}

		private void OnDisconnect(Packet packet, IPEndPoint ipEndPoint, int clientID)
		{
			// TODO: Improve checking of connected clients
			// Check if client is already disconnected
			if (!ConnectedClientsIDToIP.ContainsValue(IPEndPoint))
			{
				Logger.LogMessage(Logger.LogLevel.Warning, $"Client {clientID} ({IPEndPoint}) failed to disconnect: already disconnected.");
				return;
			}

			// Disconnect the client
			ConnectedClientsIDToIP.Remove(ConnectedClientsIPToID[IPEndPoint]);
			ConnectedClientsIPToID.Remove(IPEndPoint);

			ClientDisconnected.Invoke(ipEndPoint, clientID);
			Logger.LogMessage(Logger.LogLevel.Info, $"Client {clientID} ({IPEndPoint}) successfully disconnected.");
		}
	}
}
