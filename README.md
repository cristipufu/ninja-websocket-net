# Ninja WebSocket

NinjaWebSocket is a WebSocket wrapper with auto-reconnect and keep-alive capabilities. 
Inspired by the famous SignalR library.

## snippets

```C#
var ws = new NinjaWebSocket("wss://mainnet.infura.io/ws/v3/<api_key>");

// subscribe to events
ws.Connected += () =>
{
    Console.WriteLine("Connected");
    Console.WriteLine();

    return Task.CompletedTask;
};

ws.OnReceived += data =>
{
    Console.WriteLine(Encoding.UTF8.GetString(data!.Value.ToArray()));
    Console.WriteLine();

    return Task.CompletedTask;
};

ws.Closed += (ex) =>
{
    Console.WriteLine($"Closed: {ex?.Message}");
    Console.WriteLine();

    return Task.CompletedTask;
};

// connect to the ws and start listening
await ws.StartAsync();

// send messages
await ws.SendAsync("hello world!");
```
