using System.Buffers;

namespace Ninja.WebSocketClient
{
    public class NinjaWebSocket
    {
        private readonly WebSocketDuplexPipe _webSocketPipe;
        private readonly string _url;
        private int _keepAliveInterval;
        private int _reconnectInterval;
        private Timer? _keepAliveTimer;
        private Timer? _reconnectTimer;
        private Exception? ConnectException { get; set; }
        private Exception? CloseException { get; set; }
        private Task? ReceiveTask { get; set; }

        public ConnectionState ConnectionState { get; private set; }
        public event Func<Task>? OnConnected;
        public event Func<Exception?, Task>? OnReconnecting;
        public event Func<Task>? OnKeepAlive;
        public event Func<Exception?, Task>? OnClosed;
        public event Func<ReadOnlySequence<byte>?, Task>? OnReceived;

        public NinjaWebSocket(string url)
        {
            _url = url;
            _webSocketPipe = new WebSocketDuplexPipe();
        }

        public async Task StartAsync(CancellationToken ct = default)
        {
            try
            {
                await _webSocketPipe.StartAsync(_url, ct);

                ConnectionState = ConnectionState.Connected;

                ReceiveTask = ReceiveLoop();

                _ = OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                ConnectException = ex;

                if (ConnectionState == ConnectionState.Reconnecting)
                {
                    _ = OnReconnecting?.Invoke(ConnectException);
                }
            }
        }

        public async Task SendAsync(ArraySegment<byte> data, CancellationToken ct = default)
        {
            await _webSocketPipe.Output.WriteAsync(data, ct);
        }

        public async Task StopAsync()
        {
            _keepAliveTimer?.Dispose();
            _reconnectTimer?.Dispose();

            _webSocketPipe.Input.CancelPendingRead();

            await (ReceiveTask ?? Task.CompletedTask);

            await _webSocketPipe.StopAsync();
        }

        public NinjaWebSocket WithKeepAlive(int keepAliveIntervalSeconds = 30)
        {
            _keepAliveInterval = keepAliveIntervalSeconds;

            _keepAliveTimer = new Timer((objState) =>
            {
                if (ConnectionState != ConnectionState.Connected)
                {
                    return;
                }

                _ = KeepAlive();

            }, null, TimeSpan.FromSeconds(_keepAliveInterval), TimeSpan.FromSeconds(_keepAliveInterval));

            return this;
        }

        public NinjaWebSocket WithAutomaticReconnect(int autoReconnectIntervalSeconds = 10)
        {
            _reconnectInterval = autoReconnectIntervalSeconds;

            _reconnectTimer = new Timer((objState) =>
            {
                if (ConnectionState == ConnectionState.Connected)
                {
                    return;
                }

                ConnectionState = ConnectionState.Reconnecting;

                _ = StartAsync();

            }, null, TimeSpan.FromSeconds(_reconnectInterval), TimeSpan.FromSeconds(_reconnectInterval));

            return this;
        }

        private async Task ReceiveLoop()
        {
            var input = _webSocketPipe.Input;

            try
            {
                while (true)
                {
                    var result = await input.ReadAsync();
                    var buffer = result.Buffer;

                    try
                    {
                        if (result.IsCanceled)
                        {
                            break;
                        }

                        if (!buffer.IsEmpty)
                        {
                            _ = OnReceived?.Invoke(buffer);
                        }

                        if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        input.AdvanceTo(buffer.Start, buffer.End);
                    }
                }
            }
            catch (Exception ex)
            {
                CloseException = ex;
            }
            finally
            {
                ConnectionState = ConnectionState.Disconnected;

                _ = OnClosed?.Invoke(CloseException);
            }
        }

        private async Task KeepAlive(CancellationToken ct = default)
        {
            await _webSocketPipe.Output.WriteAsync(Memory<byte>.Empty, ct);

            _ = OnKeepAlive?.Invoke();
        }
    }
}