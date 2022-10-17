<div align="center">
<img src="docs/icon.png" alt="SteveNetworking icon/logo" width="128"/>

<h1>SteveNetworking</h1>

[![GitHub](https://img.shields.io/github/license/Steveplays28/nexlib)](https://github.com/Steveplays28/nexlib/blob/main/LICENSE)
![GitHub](https://img.shields.io/github/repo-size/Steveplays28/nexlib)
[![GitHub](https://img.shields.io/github/forks/Steveplays28/nexlib)](https://github.com/Steveplays28/nexlib/network/members)
[![GitHub](https://img.shields.io/github/issues/Steveplays28/nexlib)](https://github.com/Steveplays28/nexlib/issues)
[![GitHub](https://img.shields.io/github/issues-pr/Steveplays28/nexlib)](https://github.com/Steveplays28/nexlib/pulls)

[![Discord](https://img.shields.io/discord/746681304111906867?label=chat%20on%20Discord%20%7C%20Steve%27s%20underwater%20paradise)](https://discord.gg/KbWxgGg)
[![Twitter](https://img.shields.io/twitter/follow/Steveplays28?label=Steveplays28%20%7C%20Followers)](https://twitter.com/Steveplays28)

Lightweight UDP networking library, written in C# with .NET 6.
</div>

## Getting started
### Installation  
Download the latest release, extract it into your project, and add the following to your `.csproj` file (inside the `<Project>` tag):
```cs
<ItemGroup>
  <Reference Include="SteveNetworking">
    <HintPath>PATH\TO\SteveNetworking\SteveNetworking.dll</HintPath>
  </Reference>
</ItemGroup>
```
<sup>Make sure to change the path to the location of the dll!</sup>

This library works with any .NET 6 project.  
Example code (made for Godot 4.0):
```cs
using System.Linq;
using Godot;
using SteveNetworking.Client;
using SteveNetworking.Common;
using SteveNetworking.Server;

public partial class NetworkManager : Node
{
	public string IP { get; private set; } = "127.0.0.1";
	public int Port { get; private set; } = 23375;
	public Client Client { get; private set; }
	public Server Server { get; private set; }

	public override void _Ready()
	{
		if (OS.GetCmdlineArgs().Contains("--dedicated"))
		{
			StartServer();
		}
		else if (OS.GetCmdlineArgs().Contains("--integrated"))
		{
			StartServer();
			StartClient();
		}
		else
		{
			StartClient();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Server != null)
		{
			Server.Tick();
		}
		if (Client != null)
		{
			Client.Tick();
		}
	}

	public void StartServer()
	{
		Server = new();
		Server.Logger.Log += OnLog;
		Server.Start(Port);
	}

	public void StartClient()
	{
		Client = new();
		Client.Logger.Log += OnLog;
		Client.Start();
		Client.Connect(IP, Port);
	}

	private void OnLog(Logger.LogLevel logLevel, string logMessage)
	{
		if (logLevel == Logger.LogLevel.Info)
		{
			GD.Print(logMessage);
		}
		else if (logLevel == Logger.LogLevel.Warning)
		{
			GD.PushWarning(logMessage);
		}
		else if (logLevel == Logger.LogLevel.Error)
		{
			GD.PushError(logMessage);
		}
	}
}
```
You need to call the server/client's tick function on a set interval, so it can receive packets.
You also need to subscribe to the `Logger.Log` event with your own method to receive logs.

#### Declaring new packet types
```cs
public enum PacketTypes
{
	Input,
	Test
}
```
<sup>It's as simple as that to define new packet types.</sup>

#### Sending packets
```cs
// Create a new test packet and write to it
Packet packet = new Packet((int)PacketTypes.Test);
packet.Writer.Write("test");

// Send packet to all clients
Server.SendPacketToAll(packet);

// Send packet to specific client
Server.SendPacket(packet, clientID)

// Send packet to server
Client.SendPacket(packet)
```

#### Receiving packets
```cs
// Listen for connect packet
Server.Listen(DefaultPacketTypes.Connect, OnConnected)
Client.Listen(DefaultPacketTypes.Connect, OnConnected)

// Called after client has successfully connected
public void OnConnectPacketReceived(Packet packet, IPEndPoint IPEndPoint, int? clientID) {
	// Code goes here

	// packet is the raw packet data. Only touch this if you know what you're doing, normally you shouldn't need to use this.
	// clientID is the ID that this client got assigned. It should never be null, please send a bug report if this happens.
	// IPEndPoint is the IP address of the client/server, depending on who listened for the packet.
}
```

### Development
```
git clone https://github.com/Steveplays28/steve-networking.git
cd steve-networking
dotnet build
```

## Problems and suggestions  
If you've found a problem or want to make a suggestions, feel free to [open an issue](https://github.com/Steveplays28/nexlib/issues/new)!

Please check if there isn't already an issue open for your problem/suggestion.  
I will respond as soon as I can.

## Contributing  
If you want to add or change something, feel free to [make a pull request](https://github.com/Steveplays28/nexlib/compare)!

Please check if there isn't already a pull request open for this specific issue.  
I will respond as soon as I can.

## License  
This project is licensed under the LGPLv2.1 License, see the [LICENSE file](https://github.com/Steveplays28/nexlib/blob/main/LICENSE) for more details.
