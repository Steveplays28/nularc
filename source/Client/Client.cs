using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Nularc.Common;

namespace Nularc.Client
{
	/// <summary>
	/// UDP client which handles a connection to a server.
	/// </summary>
	public class Client
	{
		/// <summary>
		/// Event handler for when packets are received and processed.
		/// </summary>
		/// <param name="packet">The packet that was received.</param>
		public delegate void PacketReceivedEventHandler(Packet packet);
		/// <summary>
		/// Event handler for when a client has successfully (dis)connected.
		/// </summary>
		/// <param name="ipEndPoint">The IP endpoint of the client that has successfully (dis)connected.</param>
		/// <param name="clientID">The ID of the client that has successfully (dis)connected.</param>
		public delegate void ConnectedEventHandler(IPEndPoint ipEndPoint, Guid clientID);
		/// <summary>
		/// Event that gets called when a client has successfully connected.
		/// </summary>
		public event ConnectedEventHandler Connected;
		/// <summary>
		/// Event that gets called when a client has successfully disconnected.
		/// </summary>
		public event ConnectedEventHandler Disconnected;
		/// <summary>
		/// The base UDP client class that is built on top of.
		/// </summary>
		public UdpClient UdpClient { get; private set; }
		/// <summary>
		/// The IP endpoint of the server. Null when not connected to a server.
		/// </summary>
		public IPEndPoint ServerIPEndPoint { get; private set; }
		/// <summary>
		/// The IP endpoint of the local client. Null when the client isn't started.
		/// </summary>
		public IPEndPoint IPEndPoint { get; private set; }
		/// <summary>
		/// If the client has started. Some variables are <see langword="null"/> if the client isn't started, see the variables' documentation.
		/// </summary>
		public bool HasStarted { get; private set; }
		/// <summary>
		/// If the client is connected to a server. Some variables are <see langword="null"/> if the client isn't connected to a server, see the variables' documentation.
		/// </summary>
		public bool IsConnected { get; private set; }
		/// <summary>
		/// Maximum amount of packets that are processed per tick.
		/// </summary>
		public int MaxPacketsReceivedPerTick = 5;
		/// <summary>
		/// The ID of the client, used to identify the client. Set to <see cref="Guid.Empty"/> when the client is not connected to a server.
		/// </summary>
		public Guid ClientID { get; private set; } = Guid.Empty;
		/// <summary>
		/// The client's logger.
		/// </summary>
		public readonly ILogger Logger;

		/// <summary>
		/// Event that is called when packets get received and processed.
		/// </summary>
		private event PacketReceivedEventHandler PacketReceived;
		private readonly Dictionary<int, List<PacketReceivedEventHandler>> PacketListeners = new();

		/// <summary>
		/// Initialises the client.
		/// </summary>
		public Client(ILoggerFactory loggerFactory)
		{
			Logger = loggerFactory.CreateLogger("Nularc.Client");

			PacketReceived += OnPacketReceived;
			Listen((int)DefaultPacketTypes.Connect, OnConnected);
			Listen((int)DefaultPacketTypes.Disconnect, OnDisconnected);
		}

		/// <summary>
		/// Starts a new instance of the client on a random port.
		/// </summary>
		public void Start()
		{
			if (HasStarted)
			{
				Logger.LogError("Failed starting a new instance of the UDP client: the old UDP client hasn't been closed yet. Close the old UDP client before attempting to start a new UDP client.");
				return;
			}

			UdpClient = new UdpClient(0);
			IPEndPoint = (IPEndPoint)UdpClient.Client.LocalEndPoint;

			HasStarted = true;
			Logger.LogInformation("Client started successfully on {IPEndPoint}.", IPEndPoint);
		}

		/// <summary>
		/// Closes the client.
		/// </summary>
		public void Close()
		{
			if (UdpClient == null)
			{
				Logger.LogError("Failed closing the UDP client: the UDP client is null.");
				return;
			}
			if (IsConnected)
			{
				Logger.LogError("Failed closing the UDP client: the UDP client is still connected to a server. Disconnect from the server before trying to close the UDP client.");
				return;
			}

			try
			{
				UdpClient.Close();
				IPEndPoint = null;

				HasStarted = false;
				Logger.LogInformation("Successfully closed the UDP client.");
			}
			catch (SocketException e)
			{
				Logger.LogError("Failed closing the UDP client: {e}", e);
			}
		}

		/// <summary>
		/// Should run every tick, put this in your app's (framerate independent) main loop.
		/// </summary>
		public void Tick()
		{
			ReceivePackets();
		}

		/// <summary>
		/// Connects to a server with the specified IP address and port.
		/// </summary>
		/// <param name="ip">The IP address to connect to.</param>
		/// <param name="port">The port to connect to.</param>
		public void Connect(string ip, int port)
		{
			if (IsConnected)
			{
				Logger.LogWarning("Failed connecting to the server: already connected to a server. Disconnect from the currently connected server to connect to a different server.");
				return;
			}

			ServerIPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

			// Send connect packet
			using Packet packet = new((int)DefaultPacketTypes.Connect);
			SendPacket(packet);
			Logger.LogInformation("Connecting to server {ServerIPEndPoint}.", ServerIPEndPoint);
		}
		/// <summary>
		/// Connects to a server with the specified IP endpoint.
		/// </summary>
		/// <param name="ipEndPoint">The IP endpoint to connect to.</param>
		public void Connect(IPEndPoint ipEndPoint)
		{
			if (IsConnected)
			{
				Logger.LogWarning("Failed connecting to the server: already connected to a server. Disconnect from the currently connected server to connect to a different server.");
				return;
			}

			ServerIPEndPoint = ipEndPoint;

			// Send connect packet
			using Packet packet = new((int)DefaultPacketTypes.Connect);
			SendPacket(packet);
			Logger.LogInformation("Connecting to server {ServerIPEndPoint}.", ServerIPEndPoint);
		}

		/// <summary>
		/// Disconnects from the currently connected server.
		/// </summary>
		public void Disconnect()
		{
			if (!IsConnected)
			{
				Logger.LogWarning("Failed disconnecting from the server: not connected to a server.");
				return;
			}

			// Send disconnect packet
			using Packet packet = new((int)DefaultPacketTypes.Disconnect);
			SendPacket(packet);
			Logger.LogInformation("Disconnecting from server {ServerIPEndPoint}.", ServerIPEndPoint);
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
		/// Sends a packet to the server.
		/// </summary>
		/// <param name="packet">The packet to send.</param>
		/// <returns></returns>
		public void SendPacket(Packet packet)
		{
			if (!IsConnected && packet.Type != (int)DefaultPacketTypes.Connect)
			{
				Logger.LogWarning("Failed sending a packet of type {packet.Type} to the server: not connected to a server.", packet.Type);
				return;
			}
			else if (IsConnected && packet.Type == (int)DefaultPacketTypes.Connect)
			{
				Logger.LogWarning("Failed sending a {packet.Type} packet to the server: already connected to a server.", packet.Type);
				return;
			}

			// Get data from the packet
			byte[] packetData = packet.ReturnData();

			try
			{
				// Send the packet to the server
				UdpClient.Send(packetData, packetData.Length, ServerIPEndPoint);
			}
			catch (Exception e)
			{
				Logger.LogError("Failed sending a packet of type {packet.Type} to the server: {e}", packet.Type, e);
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
					// Read data from the received packet
					UdpReceiveResult udpReceiveResult = await UdpClient.ReceiveAsync();
					byte[] packetData = udpReceiveResult.Buffer;

					// Create a new packet object from the received packet data
					using Packet packet = new(packetData);

					// Invoke packet received event
					PacketReceived?.Invoke(packet);
				}
				catch (InvalidPacketHeaderException)
				{
					Logger.LogError("Received a packet with an invalid header.");
					throw;
				}
				catch (Exception)
				{
					Logger.LogError("Failed receiving a packet from the server due to an exception.");
					throw;
				}
			}
		}

		private void OnPacketReceived(Packet packet)
		{
			if (PacketListeners.ContainsKey(packet.Type))
			{
				foreach (PacketReceivedEventHandler packetReceivedEventHandler in PacketListeners[packet.Type])
				{
					packetReceivedEventHandler.Invoke(packet);
				}
			}
		}

		private void OnConnected(Packet packet)
		{
			IsConnected = true;
			ClientID = new Guid(packet.Reader.ReadBytes(16));

			Connected?.Invoke(ServerIPEndPoint, ClientID);
			Logger.LogInformation("Successfully connected to server {ServerIPEndPoint}, received client ID {ClientID}.", ServerIPEndPoint, ClientID);
		}

		private void OnDisconnected(Packet packet)
		{
			IPEndPoint serverIPEndPoint = ServerIPEndPoint;
			Guid clientID = ClientID;

			IsConnected = false;
			ServerIPEndPoint = null;
			ClientID = Guid.Empty;

			Disconnected?.Invoke(serverIPEndPoint, clientID);
			Logger.LogInformation("Successfully disconnected from server {serverIPEndPoint}.", serverIPEndPoint);
		}
	}
}
