using Newtonsoft.Json.Linq;
using System.Buffers;
using System.Text;

namespace Ninja.WebSocketClient.DemoConsole
{
    public class DiscordWebSocketClient
    {
        private NinjaWebSocket? _ws;
        private volatile int _seq = 0;

        public async Task StartAsync(string botToken)
        {
            _ws = new NinjaWebSocket("wss://gateway.discord.gg/?v=9&encoding=json")
                .SetAutomaticReconnect(intervalMilliseconds: 5000);

            _ws.OnConnected += async () =>
            {
                Console.WriteLine("Connected. Logging in...");
                Console.WriteLine();

                await _ws.SendAsync(AuthenticatePayload(botToken));
            };

            _ws.OnReceived += data =>
            {
                var message = Encoding.UTF8.GetString(data!.Value.ToArray());
                Console.WriteLine(message);
                Console.WriteLine();

                var jo = JObject.Parse(message);
                var opCode = jo?.SelectToken("op")?.ToObject<int?>();
                 
                switch (opCode)
                {
                    case 10: //Hello
                        var heartbeatInterval = jo?.SelectToken("d")?.SelectToken("heartbeat_interval")?.ToObject<int?>();
                        if (heartbeatInterval.HasValue)
                        {
                            _ws.SetKeepAlive(() => HeartbeatPayload, intervalMilliseconds: heartbeatInterval.Value);
                        }
                        break;
                    case 11: //Heartbeat ack
                        break;
                }

                var seq = jo?.SelectToken("s")?.ToObject<int?>();

                if (seq.HasValue)
                {
                    _seq = seq.Value;
                }

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

        ArraySegment<byte> HeartbeatPayload => GetPayload($"{{\"op\": 1, \"d\": {(_seq == 0 ? "null" : _seq)}}}");

        static ArraySegment<byte> AuthenticatePayload(string botToken) => GetPayload(
                @$"{{
                    ""op"": 2,
                    ""d"": {{
                    ""token"": ""{botToken}"",
                    ""intents"": 513,
                    ""properties"": {{ 
                        ""$os"": ""linux"",
                        ""$browser"": ""ninja-websocket-net"",
                        ""$device"": ""ninja-websocket-net""
                    }}}}}}");

        static ArraySegment<byte> GetPayload(string json)
        {
            var encoded = Encoding.UTF8.GetBytes(json);
            var bufferSend = new ArraySegment<byte>(encoded, 0, encoded.Length);

            return bufferSend;
        }
    }
}
