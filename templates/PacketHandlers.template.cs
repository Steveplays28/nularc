using System;
using System.Collections.Generic;

namespace NExLib
{
	public static class PacketHandlersServer
	{
		public static readonly Dictionary<int, Action<Packet>> PacketHandlers = new()
		{
			{ 0, OnConnectedInvoker },
			{ 1, OnDisconnectedInvoker }
		};

		#region OnConnected
		public delegate void ConnectedDelegate(int clientId, string messageOfTheDay);
		public static event ConnectedDelegate? OnConnected;

		private static void OnConnectedInvoker(Packet packet)
		{
			int clientId = packet.ReadInt32();
			string messageOfTheDay = packet.ReadString();

			OnConnected?.Invoke(clientId, messageOfTheDay);
		}
		#endregion

		#region OnDisconnected
		public delegate void DisconnectedDelegate();
		public static event DisconnectedDelegate? OnDisconnected;

		private static void OnDisconnectedInvoker(Packet packet)
		{
			OnDisconnected?.Invoke();
		}
		#endregion
	}

	public static class PacketHandlersClient
	{
		public static readonly Dictionary<int, Action<Packet>> PacketHandlers = new()
		{
			{ 0, OnConnectedInvoker },
			{ 1, OnDisconnectedInvoker }
		};

		#region OnConnected
		public delegate void ConnectedDelegate(int clientId, string messageOfTheDay);
		public static event ConnectedDelegate? OnConnected;

		private static void OnConnectedInvoker(Packet packet)
		{
			int clientId = packet.ReadInt32();
			string messageOfTheDay = packet.ReadString();

			OnConnected?.Invoke(clientId, messageOfTheDay);
		}
		#endregion

		#region OnDisconnected
		public delegate void DisconnectedDelegate();
		public static event DisconnectedDelegate? OnDisconnected;

		private static void OnDisconnectedInvoker(Packet packet)
		{
			OnDisconnected?.Invoke();
		}
		#endregion
	}
}
