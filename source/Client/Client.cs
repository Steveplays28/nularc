using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NExLib.Common;

namespace NExLib.Client
{
	public class Client
	{
		public UdpClient UdpClient = new UdpClient();
		public int MaxPacketsReceivedPerTick = 5;
		public IPEndPoint ServerIPEndPoint;
		public string ServerIP { get; private set; }
		public int? ServerPort { get; private set; }
		public bool IsConnected { get; private set; }
		public int ClientId { get; private set; }
		public delegate void PacketReceivedEventHandler(Packet packet);
		public event PacketReceivedEventHandler PacketReceived;
		public event PacketReceivedEventHandler Connected;
		public event PacketReceivedEventHandler Disconnected;
		public readonly LogHelper LogHelper = new LogHelper("[NExLib (Client)]: ");

		private readonly Dictionary<int, List<PacketReceivedEventHandler>> PacketListeners = new Dictionary<int, List<PacketReceivedEventHandler>>();

		public Client()
		{
			PacketReceived += OnPacketReceived;

			Listen((int)DefaultPacketTypes.Connect, OnConnected);
			Listen((int)DefaultPacketTypes.Connect, OnDisconnected);
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
			ServerIP = ip;
			ServerPort = port;

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
					IPEndPoint remoteEndPoint = udpReceiveResult.RemoteEndPoint;
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
			foreach (PacketReceivedEventHandler PacketReceivedEventHandler in PacketListeners[packet.Type])
			{
				PacketReceivedEventHandler.Invoke(packet);
			}
		}

		private void OnConnected(Packet packet)
		{
			IsConnected = true;
			ClientId = packet.Reader.ReadInt32();

			Connected.Invoke(packet);
			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Connected to server {ServerIPEndPoint}, received client ID {ClientId}.");
		}

		private void OnDisconnected(Packet packet)
		{
			IPEndPoint serverIPEndPoint = ServerIPEndPoint;

			IsConnected = false;
			ServerIPEndPoint = null;
			ServerIP = null;
			ServerPort = null;
			ClientId = 0;

			Disconnected.Invoke(packet);
			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Disconnected from server {serverIPEndPoint}");
		}
	}
}
