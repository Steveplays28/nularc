using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NExLib.Common;

namespace NExLib.Client
{
	public class Client
	{
		public int MaxPacketsReceivedPerTick = 5;
		public delegate void PacketReceivedEventHandler(Packet packet);
		public event PacketReceivedEventHandler PacketReceived;
		public UdpClient UdpClient { get; private set; }
		public IPEndPoint ServerIPEndPoint { get; private set; }
		public IPEndPoint IPEndPoint { get; private set; }
		public bool HasStarted { get; private set; }
		public bool IsConnected { get; private set; }
		public int ClientId { get; private set; }
		public readonly LogHelper LogHelper = new LogHelper("[NExLib (Client)]: ");

		private readonly Dictionary<int, List<PacketReceivedEventHandler>> PacketListeners = new Dictionary<int, List<PacketReceivedEventHandler>>();

		public Client()
		{
			PacketReceived += OnPacketReceived;

			Listen((int)DefaultPacketTypes.Connect, OnConnected);
			Listen((int)DefaultPacketTypes.Connect, OnDisconnected);
		}

		/// <summary>
		/// Initialises a new instance of the UDP client.
		/// </summary>
		public void Start()
		{
			if (UdpClient != null)
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Error, "Tried starting the UdpClient, but the UdpClient is null!");
				return;
			}

			UdpClient = new UdpClient();

			HasStarted = true;
		}

		/// <summary>
		/// Closes and disposes the UDP client.
		/// </summary>
		public void Close()
		{
			if (UdpClient == null)
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Error, "Tried closing the UdpClient, but the UdpClient is null!");
				return;
			}
			if (IsConnected)
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Error, "Tried closing the UdpClient, but the UdpClient is still connected to a server!\nDisconnect from the server before trying to close the UdpClient.");
				return;
			}

			try
			{
				UdpClient.Close();
				UdpClient.Dispose();
				LogHelper.LogMessage(LogHelper.LogLevel.Info, "Successfully closed the UdpClient.");
			}
			catch (SocketException e)
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Error, $"Failed closing the UdpClient: {e}");
			}
		}

		/// <summary>
		/// Should be ran every frame, put this in your app's main loop.
		/// </summary>
		public void Tick()
		{
			ReceivePackets();
		}

		public void Connect(string ip, int port)
		{
			if (IsConnected)
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Warning, "Failed connecting to the server: already connected to a server.");
				return;
			}

			ServerIPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

			// Send connect packet
			using (Packet packet = new Packet((int)DefaultPacketTypes.Connect))
			{
				SendPacket(packet);
				LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Connecting to server {ServerIPEndPoint}");
			}
		}

		public void Disconnect()
		{
			if (!IsConnected)
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Warning, "Failed disconnecting from the server: not connected to a server.");
				return;
			}

			// Send disconnect packet
			using (Packet packet = new Packet((int)DefaultPacketTypes.Disconnect))
			{
				SendPacket(packet);
				LogHelper.LogMessage(LogHelper.LogLevel.Info, "Sent disconnect packet to the server.");
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
		/// Sends a packet to the server.
		/// </summary>
		/// <param name="packet">The packet to send.</param>
		/// <returns></returns>
		public void SendPacket(Packet packet)
		{
			if (!IsConnected && packet.Type != (int)DefaultPacketTypes.Connect)
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Error, "Tried sending packet to server while client is not connected.");
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
				LogHelper.LogMessage(LogHelper.LogLevel.Error, $"Error occurred while trying to send packet to server: {e}\nCheck if the client is connected to the server.");
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
							PacketReceived.Invoke(packet);
						}
					}
				}
				catch (Exception e)
				{
					LogHelper.LogMessage(LogHelper.LogLevel.Error, $"Error occurred while trying to receive packet from server: {e}");
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

			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Connected to server {ServerIPEndPoint}, received client ID {ClientId}.");
		}

		private void OnDisconnected(Packet packet)
		{
			IPEndPoint serverIPEndPoint = ServerIPEndPoint;

			IsConnected = false;
			ServerIPEndPoint = null;
			ClientId = 0;

			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Disconnected from server {serverIPEndPoint}");
		}
	}
}
