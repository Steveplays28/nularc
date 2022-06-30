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
		public delegate void PacketCallback(Packet packet, IPEndPoint clientIPEndPoint);
		public event PacketCallback PacketReceived;

		public readonly LogHelper LogHelper = new LogHelper("[NExLib (Client)]: ");

		private IPEndPoint serverEndPoint;

		public void Close()
		{
			if (UdpClient == null)
			{
				return;
			}

			try
			{
				UdpClient.Close();
				LogHelper.LogMessage(LogHelper.LogLevel.Info, "Successfully closed the UdpClient!");
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

			PacketReceived += PacketReceivedHandler;
			ServerIp = ip;
			ServerPort = port;
			serverEndPoint = new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort);

			// Send connect packet
			using (Packet packet = new Packet((int)PacketMethod.Connect))
			{
				SendPacket(packet);
				LogHelper.LogMessage(LogHelper.LogLevel.Info, "Sent connect packet to the server.");
			}
		}

		public void Disconnect()
		{
			if (!IsConnected)
			{
				LogHelper.LogMessage(LogHelper.LogLevel.Warning, "Failed disconnecting from the server: not connected to a server.");
				return;
			}

			IsConnected = false;
			serverEndPoint = null;

			// Send disconnect packet
			using (Packet packet = new Packet((int)PacketMethod.Disconnect))
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
			// Get data from the packet
			byte[] packetData = packet.ReturnData();

			// Send the packet to the server
			UdpClient.Send(packetData, packetData.Length, serverEndPoint);
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
				// Extract data from the received packet
				UdpReceiveResult udpReceiveResult = await UdpClient.ReceiveAsync();
				IPEndPoint remoteEndPoint = udpReceiveResult.RemoteEndPoint;
				byte[] packetData = udpReceiveResult.Buffer;

				// Create new Packet object from the received packet data and invoke PacketReceived event
				using (Packet packet = new Packet(packetData))
				{
					if (PacketReceived != null)
					{
						LogHelper.LogMessage(LogHelper.LogLevel.Info, string.Join(", ", packetData));
						LogHelper.LogMessage(LogHelper.LogLevel.Info, packet.ConnectedMethod.ToString());
						PacketReceived.Invoke(packet, remoteEndPoint);
					}
				}
			}
		}

		private void PacketReceivedHandler(Packet packet, IPEndPoint serverIPEndPoint)
		{
			if (packet.ConnectedMethod == (int)PacketMethod.Connect)
			{
				Connected(packet, serverIPEndPoint);
				return;
			}

			if (packet.ConnectedMethod == (int)PacketMethod.Disconnect)
			{
				Disconnected(serverIPEndPoint);
				return;
			}
		}

		private void Connected(Packet packet, IPEndPoint serverIPEndPoint)
		{
			ClientId = packet.Reader.ReadInt32();
			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Connected to server {serverIPEndPoint}, received client ID {ClientId}.");
		}

		private void Disconnected(IPEndPoint serverIPEndPoint)
		{
			LogHelper.LogMessage(LogHelper.LogLevel.Info, $"Disconnected from server {serverIPEndPoint}");
		}
	}
}
