using Ninja.WebSocketClient.DemoConsole;

var ethereumWebSocket = new EthereumWebSocketClient();

await ethereumWebSocket.StartAsync(apiKey: "<your_api_key>");

var discordWebSocket = new DiscordWebSocketClient();

await discordWebSocket.StartAsync(botToken: "<your_bots_token>");

Thread.Sleep(-1);