using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NExLib.Common;

namespace NExLib.Client
{
	public class Client
	{
		public const string DefaultServerIp = "127.0.0.1";
		public const int DefaultServerPort = 24465;
		public const int MaxPacketsReceivedPerTick = 5;

		public UdpClient UdpClient = new UdpClient();
		public string ServerIp { get; private set; } = DefaultServerIp;
		public int ServerPort { get; private set; } = DefaultServerPort;
		public bool IsConnected { get; private set; }
		public int ClientId { get; private set; }
		public delegate void PacketCallback(Packet packet, IPEndPoint serverIPEndPoint);
		public event PacketCallback PacketReceived;
		public event PacketCallback Connected;
		public event PacketCallback Disconnected;

		public readonly LogHelper LogHelper = new LogHelper("[NExLib (Client)]: ");

		private IPEndPoint serverEndPoint;
		private readonly Dictionary<int, List<PacketCallback>> PacketListeners = new Dictionary<int, List<PacketCallback>>();

		public Client()
		{
			PacketReceived += PacketReceivedWrapper;

			Listen((int)DefaultPacketTypes.Connect, ConnectedHandler);
			Listen((int)DefaultPacketTypes.Connect, DisconnectedHandler);
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

			ServerIp = ip;
			ServerPort = port;
			serverEndPoint = new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort);

			// Send connect packet
			using (Packet packet = new Packet((int)DefaultPacketTypes.Connect))
			{
				SendPacket(packet);
				LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Connecting to server {serverEndPoint}");
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
				UdpClient.Send(packetData, packetData.Length, serverEndPoint);
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
				LogHelper.LogMessage(LogHelper.LogLevel.Error, "Tried receiving packets, but the UdpClient is null!");
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

					// Create new Packet object from the received packet data and invoke PacketReceived event
					using (Packet packet = new Packet(packetData))
					{
						if (PacketReceived != null)
						{
							PacketReceived.Invoke(packet, remoteEndPoint);
						}
					}
				}
				catch (Exception e)
				{
					LogHelper.LogMessage(LogHelper.LogLevel.Error, $"Error occurred while trying to receive packet from server: {e}");
				}
			}
		}

		private void Listen(int packetType, PacketCallback method)
		{
			if (!PacketListeners.ContainsKey(packetType))
			{
				var packetCallbacks = new List<PacketCallback>
				{
					method
				};

				PacketListeners.Add(packetType, packetCallbacks);
				return;
			}

			PacketListeners[packetType].Add(method);
		}

		private void PacketReceivedWrapper(Packet packet, IPEndPoint serverIPEndPoint)
		{
			foreach (PacketCallback packetCallback in PacketListeners[packet.Type])
			{
				packetCallback.Invoke(packet, serverIPEndPoint);
			}
		}

		private void ConnectedHandler(Packet packet, IPEndPoint serverIPEndPoint)
		{
			if (packet.Type != (int)DefaultPacketTypes.Connect)
			{
				return;
			}

			IsConnected = true;
			ClientId = packet.Reader.ReadInt32();

			Connected.Invoke(packet, serverIPEndPoint);
			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Connected to server {serverIPEndPoint}, received client ID {ClientId}.");
		}

		private void DisconnectedHandler(Packet packet, IPEndPoint serverIPEndPoint)
		{
			if (packet.Type != (int)DefaultPacketTypes.Disconnect)
			{
				return;
			}

			IsConnected = false;
			serverEndPoint = null;
			ClientId = 0;

			Disconnected.Invoke(packet, serverIPEndPoint);
			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Disconnected from server {serverIPEndPoint}");
		}
	}
}
