<h1 align="center">
NExLib
</h1>

<div align="center">

[![GitHub](https://img.shields.io/github/license/Steveplays28/nexlib)](https://github.com/Steveplays28/nexlib/blob/main/LICENSE)
![GitHub](https://img.shields.io/github/repo-size/Steveplays28/nexlib)
[![GitHub](https://img.shields.io/github/forks/Steveplays28/nexlib)](https://github.com/Steveplays28/nexlib/network/members)
[![GitHub](https://img.shields.io/github/issues/Steveplays28/nexlib)](https://github.com/Steveplays28/nexlib/issues)
[![GitHub](https://img.shields.io/github/issues-pr/Steveplays28/nexlib)](https://github.com/Steveplays28/nexlib/pulls)

[![Discord](https://img.shields.io/discord/746681304111906867?label=chat%20on%20Discord%20%7C%20Steve%27s%20underwater%20paradise)](https://discord.gg/KbWxgGg)
[![Custom](https://img.shields.io/badge/I%20support-Ukraine-yellow?labelColor=0000FF)](https://www.cfr.org/global-conflict-tracker/conflict/conflict-ukraine)

Lightweight C# UDP networking library with an event system.
</div>

## Getting started

### Installation

1. Make 2 new projects, one for the server, and one for the client.
2. Download the server, client, and shared files from the [latest release](https://github.com/Steveplays28/nexlib/releases/latest).
3. Extract the zip files, and put the folders in your project directory.
4. You can now call the library's functions (`Server`, `Client`) by importing it: `using NExLib;`.

### Usage

#### Server side

ServerController.cs
```cs
using NExLib;

public class ServerController : Node
{
	// The ready function that gets called when the game/app starts, will be different per engine
	public override void _Ready()
	{
		// Subscribe to events
		PacketCallbacksServer.OnConnected += OnConnected;
		PacketCallbacksServer.OnDisconnected += OnDisconnected;

		// Start the server on 127.0.0.1:24465
		Server.Start(24465);
	}

	// Gets invoked after the client has connected to a server
	private void OnConnected()
	{
		// Your code here
	}

	// Gets invoked after the client has disconnected from a server
	private void OnDisconnected()
	{
		// Your code here
	}
}
```

PacketCallbacksServer.cs
```cs
using System.Collections.Generic;
using NExLib;

public static class PacketCallbacksServer
{
	public static Dictionary<int, Action<Packet>> PacketCallbacks = new Dictionary<int, Action<Packet>>()
	{
		{ 0, OnConnectedInvoker },
		{ 1, OnDisconnectedInvoker }
	};

	#region OnConnected
	public delegate void ConnectedDelegate(int clientId, string messageOfTheDay);
	public static event ConnectedDelegate OnConnected;

	private static void OnConnectedInvoker(Packet packet)
	{
		int clientId = packet.ReadInt32();
		string messageOfTheDay = packet.ReadString();

		OnConnected.Invoke(clientId, messageOfTheDay);
	}
	#endregion

	#region OnDisconnected
	public delegate void DisconnectedDelegate();
	public static event DisconnectedDelegate OnDisconnected;

	private static void OnDisconnectedInvoker(Packet packet)
	{
		OnDisconnected.Invoke();
	}
	#endregion
}
```

#### Client side

ClientController.cs
```cs
using NExLib;

public class ClientController : Node
{
	// The ready function that gets called when the game/app starts, will be different per engine
	public override void _Ready()
	{
		// Subscribe to events
		PacketCallbacksClient.OnConnected += OnConnected;
		PacketCallbacksClient.OnDisconnected += OnDisconnected;

		// Initialize the client
		Client.Initialize();

		// Connect to the server on 127.0.0.1:24465
		Client.Connect("12.0.0.1", 24465)
	}

	// Gets invoked after the client has connected to a server
	private void OnConnected()
	{
		// Your code here
	}

	// Gets invoked after the client has disconnected from a server
	private void OnDisconnected()
	{
		// Your code here
	}
}
```

PacketCallbacksClient.cs
```cs
using System.Collections.Generic;
using NExLib;

public static class PacketCallbacksClient
{
	public static Dictionary<int, Action<Packet>> PacketCallbacks = new Dictionary<int, Action<Packet>>()
	{
		{ 0, OnConnectedInvoker },
		{ 1, OnDisconnectedInvoker }
	};

	#region OnConnected
	public delegate void ConnectedDelegate(int clientId, string messageOfTheDay);
	public static event ConnectedDelegate OnConnected;

	private static void OnConnectedInvoker(Packet packet)
	{
		int clientId = packet.ReadInt32();
		string messageOfTheDay = packet.ReadString();

		OnConnected.Invoke(clientId, messageOfTheDay);
	}
	#endregion

	#region OnDisconnected
	public delegate void DisconnectedDelegate();
	public static event DisconnectedDelegate OnDisconnected;

	private static void OnDisconnectedInvoker(Packet packet)
	{
		OnDisconnected.Invoke();
	}
	#endregion
}
```

### Example projects

There's an example project available for this library, split into two repositories:
- [Server side](https://github.com/Steveplays28/networking-example-server)
- [Client side](https://github.com/Steveplays28/networking-example-client)


## FAQ

**Q: What does the name stand for?**

A: A friend ([M1x3l](https://github.com/M1x3l)) suggested it to me, it stands for NetworkingExampleLibrary, since it's so simple. Haha

**Q: Where can I use this?**

A: You can use this in any project that uses C#. The logger only supports Godot, Unity, and the .NET console.

## Problems and suggestions

If you've found a problem or want to make a suggestions, feel free to [open an issue](https://github.com/Steveplays28/nexlib/issues/new)!

Please check if there isn't already an issue open for your problem/suggestion.

I will respond as soon as I can.


## Contributing

If you want to add or change something, feel free to [make a pull request](https://github.com/Steveplays28/nexlib/compare)!

Please check if there isn't already a pull request open for this specific issue.

I will respond as soon as I can.


## My links

Feel free to contact me via any of these platforms, I respond quite quickly :)

[![Discord](https://img.shields.io/discord/746681304111906867?label=chat%20on%20Discord%20%7C%20Steve%27s%20underwater%20paradise&style=social&logo=discord)](https://discord.gg/KbWxgGg)

[![GitHub](https://img.shields.io/github/stars/Steveplays28?label=Steveplays28%20%7C%20Stars&style=social)](https://github.com/Steveplays28)

[![YouTube](https://img.shields.io/youtube/channel/subscribers/UC0GP9rATvC5L8yH_NrCaBJw?label=Steveplays%20%7C%20Subscribers&style=social)](https://youtube.com/c/Steveplays28)

[![Twitter](https://img.shields.io/twitter/follow/Steveplays28?label=Steveplays28%20%7C%20Followers&style=social)](https://twitter.com/Steveplays28)

[![Reddit](https://img.shields.io/reddit/user-karma/combined/Steveplays28?label=Steveplays28%20%7C%20Karma&style=social)](https://reddit.com/u/Steveplays28)
