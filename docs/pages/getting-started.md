# Getting started

Download the latest release, extract it into your project, and add the following to your `.csproj` file (inside the `<Project>` tag):

```cs
<ItemGroup>
  <Reference Include="SteveNetworking">
    <HintPath>PATH\TO\SteveNetworking\SteveNetworking.dll</HintPath>
  </Reference>
</ItemGroup>
```

!> Make sure to change the path to the location of the dll!

## Usage

<!-- tabs:start -->
#### **Godot 4.0 beta 1**

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

#### **Unity**

```cs
// TODO
```
<!-- tabs:end -->

#### Declaring new packet types

Create a new file `PacketTypes.cs` and add the following code to it:

```cs
public enum PacketTypes
{
 Input,
 Test
}
```

## Sending packets

<!-- tabs:start -->
#### **Godot 4.0 beta 1**

```cs
using Godot;
using SteveNetworking.Client;
using SteveNetworking.Server;

public partial class NetworkManager : Node
{
 public string IP { get; private set; } = "127.0.0.1";
 public int Port { get; private set; } = 23375;
 public Client Client { get; private set; }
 public Server Server { get; private set; }

 public override void _Ready()
 {
  // ...

  bool currentInput = Input.GetActionPressed("move_forward");

  using Packet packet = new Packet((int)PacketTypes.Input);
  packet.Writer.Write(currentInput);

  Client.SendPacket(packet);
 }

 // ...
}
```

#### **Unity**

```cs
// TODO
```
<!-- tabs:end -->

## Receiving packets

<!-- tabs:start -->
#### **Godot 4.0 beta 1**

```cs
using Godot;
using SteveNetworking.Client;
using SteveNetworking.Server;

public partial class NetworkManager : Node
{
 public string IP { get; private set; } = "127.0.0.1";
 public int Port { get; private set; } = 23375;
 public Client Client { get; private set; }
 public Server Server { get; private set; }

 public override void _Ready()
 {
  // ...

  Server.Listen((int)PacketTypes.Input, OnPlayerInput);
 }

 private void OnPlayerInput(Packet packet, IPEndPoint ipEndPoint, int clientID)
 {
    bool currentInput = packet.Reader.ReadBoolean();
    // Handle player input here
 }

 // ...
}
```

#### **Unity**

```cs
// TODO
```
<!-- tabs:end -->
