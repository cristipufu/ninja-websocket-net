using Ninja.WebSocketClient;
using System.Buffers;
using System.Text;

var ws = new NinjaWebSocket("wss://mainnet.infura.io/ws/v3/<api_key>")
    .WithKeepAlive(keepAliveIntervalSeconds: 5)
    .WithAutomaticReconnect(autoReconnectIntervalSeconds: 5);

ws.OnConnected += async () =>
{
    // Notify users the connection was established.
    Console.WriteLine("Connected:");

    // Subscribe to Infura wss endpoint.
    await ws.SendAsync(GetSubscriptionJson());
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

await ws.StartAsync();

Thread.Sleep(-1);

static ArraySegment<byte> GetSubscriptionJson()
{
    var subscription = $"{{\"jsonrpc\":\"2.0\", \"id\": 1, \"method\": \"eth_subscribe\", \"params\": [\"logs\", {{\"address\": \"0x7deb7bce4d360ebe68278dee6054b882aa62d19c\"}}]}}";

    var encoded = Encoding.UTF8.GetBytes(subscription);
    var bufferSend = new ArraySegment<byte>(encoded, 0, encoded.Length);

    return bufferSend;
}