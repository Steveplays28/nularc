# Logging

Learn how to use the abstract logging implementation for easier debugging.

<!-- tabs:start -->
<!-- markdownlint-disable-next-line no-duplicate-header -->
## **Godot 4**

First, we make a `GodotLogger` class, which will handle the logging for Godot.  
These logs are formatted as `[CategoryName]: log message`.

`GodotLogger.cs`

```cs
using System;
using System.Collections.Generic;
using Godot;
using Microsoft.Extensions.Logging;

public sealed class GodotLogger : ILogger
{
 public readonly string Name;
 public readonly GodotLoggerConfiguration Config;

 public GodotLogger(string name, GodotLoggerConfiguration config)
 {
  (Name, Config) = (name, config);
 }

 public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;

 public bool IsEnabled(LogLevel logLevel)
 {
  return Config.LogLevelEnabledMap.GetValueOrDefault(logLevel);
 }

 public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
 {
  if (!IsEnabled(logLevel))
  {
   return;
  }

  switch (logLevel)
  {
   case LogLevel.Warning:
    GD.PushWarning($"[{Name}]: {formatter(state, exception)}");
    return;
   case LogLevel.Error:
    GD.PushError($"[{Name}]: {formatter(state, exception)}");
    return;
   case LogLevel.Critical:
    GD.PushError($"[{Name}]: {formatter(state, exception)}");
    return;
  }

  GD.Print($"[{Name}]: {formatter(state, exception)}");
 }
}
```

Next, we create a `GodotLoggerConfiguration` class for the Godot logger. This class will tell the Godot logger which log levels should be shown in the console.  
By default, it's set to only show logs in debug builds. You should customize this to your liking/needs.

`GodotLoggerConfiguration.cs`

```cs
using System.Collections.Generic;
using Godot;
using Microsoft.Extensions.Logging;

public sealed class GodotLoggerConfiguration
{
 public Dictionary<LogLevel, bool> LogLevelEnabledMap { get; set; } = new()
 {
  [LogLevel.Debug] = OS.IsDebugBuild(),
  [LogLevel.Trace] = OS.IsDebugBuild(),
  [LogLevel.Information] = OS.IsDebugBuild(),
  [LogLevel.Warning] = OS.IsDebugBuild(),
  [LogLevel.Error] = OS.IsDebugBuild(),
  [LogLevel.Critical] = OS.IsDebugBuild(),
 };
}
```

Last, we create a `GodotLoggerProvider` class which will create a `ILoggerProvider` that we can use to provide C# with our new logger.

`GodotLoggerProvider.cs`

```cs
using System;
using Microsoft.Extensions.Logging;

public sealed class GodotLoggerProvider : ILoggerProvider
{
 public ILogger CreateLogger(string categoryName)
 {
  return new GodotLogger(categoryName, new GodotLoggerConfiguration());
 }

 public void Dispose()
 {
  GC.SuppressFinalize(this);
 }
}
```

Now it's time for the implementation, so in our [`NetworkManager` class (see examples)](examples.md) we should create a `LoggerFactory` that we can pass into our `Client` and `Server`.

`NetworkManager.cs`

```cs
 // ...

 public void StartServer()
 {
  Server = new(CreateLoggerFactory());
  Server.Start(Port);
 }

 public void StartClient()
 {
  Client = new(CreateLoggerFactory());
  Client.Start();

  Client.Connect(IP, Port);
 }

 private static ILoggerFactory CreateLoggerFactory()
 {
  return LoggerFactory.Create(builder =>
  {
   builder.AddProvider(new GodotLoggerProvider());
  });
 }
```
<!-- tabs:end -->
