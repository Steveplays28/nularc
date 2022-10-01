using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using SteveNetworking.Common;

namespace SteveNetworking.Client
{
	/// <summary>
	/// UDP client which handles a connection to a server.
	/// </summary>
	public class Client
	{
		/// <summary>
		/// Maximum amount of packets that are processed per tick.
		/// </summary>
		public int MaxPacketsReceivedPerTick = 5;
		/// <summary>
		/// Event handler for when packets are received and processed.
		/// </summary>
		/// <param name="packet">The packet that was received.</param>
		public delegate void PacketReceivedEventHandler(Packet packet);
		/// <summary>
		/// Event that is called when packets get received and processed.
		/// </summary>
		public event PacketReceivedEventHandler PacketReceived;
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
		/// The ID of the client, used to identify the client.
		/// </summary>
		public int ClientId { get; private set; }
		/// <summary>
		/// The client's logger.
		/// </summary>
		public readonly LogHelper LogHelper = new("[SteveNetworking (Client)]: ");

		private readonly Dictionary<int, List<PacketReceivedEventHandler>> PacketListeners = new();

		/// <summary>
		/// Initialises the client.
		/// </summary>
		public Client()
		{
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
				LogHelper.LogMessage(LogHelper.LogLevel.Error, "Failed starting a new instance of the UDP client: the old UDP client hasn't been closed yet. Close the old UDP client before attempting to start a new UDP client.");
				return;
			}

			UdpClient = new UdpClient();
			IPEndPoint = (IPEndPoint)UdpClient.Client.LocalEndPoint;

			HasStarted = true;
		}

		/// <summary>
		/// Closes the client.
		/// </summary>
		public void Close()
		{
			if (UdpClient == null)
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Error, "Failed closing the UDP client: the UDP client is null.");
				return;
			}
			if (IsConnected)
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Error, "Failed closing the UDP client: the UDP client is still connected to a server. Disconnect from the server before trying to close the UDP client.");
				return;
			}

			try
			{
				UdpClient.Close();
				IPEndPoint = null;

				HasStarted = false;
				LogHelper.LogMessage(LogHelper.LogLevel.Info, "Successfully closed the UDP client.");
			}
			catch (SocketException e)
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Error, $"Failed closing the UDP client: {e}");
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
		/// Connects to a server with the specified IP address and port.
		/// </summary>
		/// <param name="ip">The IP address to connect to.</param>
		/// <param name="port">The port to connect to.</param>
		public void Connect(string ip, int port)
		{
			if (IsConnected)
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Warning, "Failed connecting to the server: already connected to a server. Disconnect from the currently connected server to connect to a different server.");
				return;
			}

			ServerIPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

			// Send connect packet
			using Packet packet = new((int)DefaultPacketTypes.Connect);
			SendPacket(packet);
			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Connecting to server {ServerIPEndPoint}.");
		}
		/// <summary>
		/// Connects to a server with the specified IP endpoint.
		/// </summary>
		/// <param name="IPEndPoint">The IP endpoint to connect to.</param>
		public void Connect(IPEndPoint IPEndPoint)
		{
			if (IsConnected)
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Warning, "Failed connecting to the server: already connected to a server. Disconnect from the currently connected server to connect to a different server.");
				return;
			}

			ServerIPEndPoint = IPEndPoint;

			// Send connect packet
			using Packet packet = new((int)DefaultPacketTypes.Connect);
			SendPacket(packet);
			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Connecting to server {ServerIPEndPoint}.");
		}

		/// <summary>
		/// Disconnects from the currently connected server.
		/// </summary>
		public void Disconnect()
		{
			if (!IsConnected)
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Warning, "Failed disconnecting from the server: not connected to a server.");
				return;
			}

			// Send disconnect packet
			using Packet packet = new((int)DefaultPacketTypes.Disconnect);
			SendPacket(packet);
			LogHelper.LogMessage(LogHelper.LogLevel.Info, "Disconnecting from server {ServerIPEndPoint}.");
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
				LogHelper.LogMessage(LogHelper.LogLevel.Warning, $"Failed sending a packet of type {packet.Type} to the server: not connected to a server.");
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
				LogHelper.LogMessage(LogHelper.LogLevel.Error, $"Failed sending a packet of type {packet.Type} to the server: {e}");
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

					// Check if the packet is a user defined packet, and if so, contains a header and data
					if (packet.Type >= 0)
					{
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
					}

					// Invoke packet received event
					if (PacketReceived != null)
					{
						PacketReceived.Invoke(packet);
					}
				}
				catch (Exception e)
				{
					// TODO: Improve packet receive failure log message
					LogHelper.LogMessage(LogHelper.LogLevel.Error, $"Failed receiving a packet from the server: {e}");
				}
			}
		}

		private void OnPacketReceived(Packet packet)
		{
			foreach (PacketReceivedEventHandler packetReceivedEventHandler in PacketListeners[packet.Type])
			{
				packetReceivedEventHandler.Invoke(packet);
			}
		}

		private void OnConnected(Packet packet)
		{
			IsConnected = true;
			ClientId = packet.Reader.ReadInt32();

			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Successfully connected to server {ServerIPEndPoint}, received client ID {ClientId}.");
		}

		private void OnDisconnected(Packet packet)
		{
			IPEndPoint serverIPEndPoint = ServerIPEndPoint;

			IsConnected = false;
			ServerIPEndPoint = null;
			ClientId = 0;

			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Successfully disconnected from server {serverIPEndPoint}.");
		}
	}
}
