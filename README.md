# Ninja WebSocket

NinjaWebSocket is an easy-to-use .NET 6 WebSocket client with auto-reconnect and keep-alive capabilities. 

Lightweight library, user-friendly API, inspired by the javascript WebSocket API and SignalR.

## snippets

```C#
var ws = new NinjaWebSocket("wss://mainnet.infura.io/ws/v3/<api_key>")
    .WithKeepAlive(keepAliveIntervalSeconds: 5)
    .WithAutomaticReconnect(autoReconnectIntervalSeconds: 5);

ws.OnConnected += async () =>
{
    // Notify users the connection was established.
    Console.WriteLine("Connected:");

    // Send messages
    await ws.SendAsync("hello world!");
};

ws.OnReceived += data =>
{
    Console.WriteLine(Encoding.UTF8.GetString(data!.Value.ToArray()));

    return Task.CompletedTask;
};

ws.OnKeepAlive += () =>
{
    Console.WriteLine("Ping.");

    return Task.CompletedTask;
};

ws.OnReconnecting += (ex) =>
{
    // Notify users the connection was lost and the client is reconnecting.
    Console.WriteLine($"Reconnecting: {ex?.Message}");

    return Task.CompletedTask;
};

ws.OnClosed += (ex) =>
{
    // Notify users the connection has been closed.
    Console.WriteLine($"Closed: {ex?.Message}");

    return Task.CompletedTask;
};

// Connect to the ws and start listening
await ws.StartAsync();

```
