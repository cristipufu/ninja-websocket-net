using System.Buffers;
using System.Text;

namespace Ninja.WebSocketClient.DemoConsole
{
    public class EthereumWebSocketClient
    {
        private NinjaWebSocket? _ws;

        public async Task StartAsync(string apiKey)
        {
            _ws = new NinjaWebSocket($"wss://mainnet.infura.io/ws/v3/{apiKey}")
                .SetKeepAlive(intervalMilliseconds: 5000)
                .SetAutomaticReconnect(intervalMilliseconds: 5000);

            _ws.OnConnected += async () =>
            {
                Console.WriteLine("Connected.");
                // Subscribe to Ethereum wss endpoint.
                await _ws.SendAsync(GetSubscription());
            };

            _ws.OnReceived += data =>
            {
                var message = Encoding.UTF8.GetString(data!.Value.ToArray());
                Console.WriteLine(message);
                return Task.CompletedTask;
            };

            _ws.OnKeepAlive += () =>
            {
                Console.WriteLine("Ping.");
                return Task.CompletedTask;
            };

            _ws.OnReconnecting += (ex) =>
            {
                Console.WriteLine($"Reconnecting: {ex?.Message}");
                return Task.CompletedTask;
            };

            _ws.OnClosed += (ex) =>
            {
                Console.WriteLine($"Closed: {ex?.Message}");
                return Task.CompletedTask;
            };

            await _ws.StartAsync();
        }

        public async Task StopAsync()
        {
            await (_ws?.StopAsync() ?? Task.CompletedTask);
        }

        static ArraySegment<byte> GetSubscription()
        {
            var subscription = $"{{\"jsonrpc\":\"2.0\", \"id\": 1, \"method\": \"eth_subscribe\", \"params\": [\"logs\", {{\"address\": \"0x7deb7bce4d360ebe68278dee6054b882aa62d19c\"}}]}}";

            return GetPayload(subscription);
        }

        static ArraySegment<byte> GetPayload(string json)
        {
            var encoded = Encoding.UTF8.GetBytes(json);
            var bufferSend = new ArraySegment<byte>(encoded, 0, encoded.Length);

            return bufferSend;
        }
    }
}
