using Ninja.WebSocketClient;
using System.Buffers;
using System.Text;

var ws = new NinjaWebSocket("wss://mainnet.infura.io/ws/v3/<api_key>");

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

await ws.StartAsync();

await ws.SendAsync(GetSubscriptionJson());

Console.ReadLine();

static ArraySegment<byte> GetSubscriptionJson()
{
    var subscription = $"{{\"jsonrpc\":\"2.0\", \"id\": 1, \"method\": \"eth_subscribe\", \"params\": [\"logs\", {{\"address\": \"0x7deb7bce4d360ebe68278dee6054b882aa62d19c\"}}]}}";

    var encoded = Encoding.UTF8.GetBytes(subscription);
    var bufferSend = new ArraySegment<byte>(encoded, 0, encoded.Length);

    return bufferSend;
}