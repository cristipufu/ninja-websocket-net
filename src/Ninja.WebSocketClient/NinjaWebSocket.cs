using System.Buffers;

namespace Ninja.WebSocketClient
{
    public class NinjaWebSocket
    {
        private readonly WebSocketDuplexPipe _webSocketPipe;
        private readonly string _url;
        private Exception? CloseException { get; set; }

        private Task? ReceiveTask { get; set; }

        public event Func<Exception?, Task>? Closed;

        public event Func<Task>? Connected;

        public event Func<ReadOnlySequence<byte>?, Task>? OnReceived;

        public NinjaWebSocket(string url)
        {
            _url = url;
            _webSocketPipe = new WebSocketDuplexPipe();
        }

        public async Task StartAsync(CancellationToken ct = default)
        {
            await _webSocketPipe.StartAsync(_url, ct);

            ReceiveTask = ReceiveLoop();

            await (Connected?.Invoke() ?? Task.CompletedTask);
        }

        public async Task SendAsync(ArraySegment<byte> data, CancellationToken ct = default)
        {
            await _webSocketPipe.Output.WriteAsync(data, ct);
        }

        public async Task StopAsync()
        {
            _webSocketPipe.Input.CancelPendingRead();

            await (ReceiveTask ?? Task.CompletedTask);

            await _webSocketPipe.StopAsync();
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
                            await (OnReceived?.Invoke(buffer) ?? Task.CompletedTask);
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
                await (Closed?.Invoke(CloseException) ?? Task.CompletedTask);
            }
        }
    }
}