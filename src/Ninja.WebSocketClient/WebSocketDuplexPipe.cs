using System.IO.Pipelines;
using System.Net.WebSockets;

namespace Ninja.WebSocketClient
{
    internal class WebSocketDuplexPipe : IDuplexPipe
    {
        private ClientWebSocket? _webSocket;
        private IDuplexPipe? _transport;
        private IDuplexPipe? _application;
        private volatile bool _aborted;

        internal Task Running { get; private set; } = Task.CompletedTask;

        public PipeReader Input => _transport!.Input;

        public PipeWriter Output => _transport!.Output;

        public WebSocketDuplexPipe()
        {

        }

        public async Task StartAsync(string url, CancellationToken ct = default)
        {
            _webSocket = new ClientWebSocket();

            try
            {
                await _webSocket.ConnectAsync(new Uri(url), ct);
            }
            catch
            {
                _webSocket.Dispose();
                throw;
            }

            var input = new Pipe();
            var output = new Pipe();

            _transport = new DuplexPipe(output.Reader, input.Writer);
            _application = new DuplexPipe(input.Reader, output.Writer);

            Running = ProcessSocketAsync(_webSocket);
        }

        private async Task ProcessSocketAsync(WebSocket socket)
        {
            using (socket)
            {
                var receiving = StartReceiving(socket);
                var sending = StartSending(socket);

                var trigger = await Task.WhenAny(receiving, sending);

                if (trigger == receiving)
                {
                    _application!.Input.CancelPendingRead();

                    using var delayCts = new CancellationTokenSource();

                    var resultTask = await Task.WhenAny(sending, Task.Delay(TimeSpan.FromSeconds(5), delayCts.Token));

                    if (resultTask != sending)
                    {
                        _aborted = true;

                        socket.Abort();
                    }
                    else
                    {
                        delayCts.Cancel();
                    }
                }
                else
                {
                    _aborted = true;

                    socket.Abort();

                    _application!.Output.CancelPendingFlush();
                }
            }
        }

        private async Task StartReceiving(WebSocket socket)
        {
            try
            {
                while (true)
                {
                    var memory = _application!.Output.GetMemory();

                    var receiveResult = await socket.ReceiveAsync(memory, CancellationToken.None);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        if (socket.CloseStatus != WebSocketCloseStatus.NormalClosure)
                        {
                            throw new InvalidOperationException($"Websocket closed with error: {socket.CloseStatus}.");
                        }

                        return;
                    }

                    _application.Output.Advance(receiveResult.Count);

                    var flushResult = await _application.Output.FlushAsync();

                    if (flushResult.IsCanceled || flushResult.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                if (!_aborted)
                {
                    _application!.Output.Complete(ex);
                }
            }
            finally
            {
                _application!.Output.Complete();
            }
        }

        private async Task StartSending(WebSocket socket)
        {
            Exception? error = null;

            try
            {
                while (true)
                {
                    var result = await _application!.Input.ReadAsync();
                    var buffer = result.Buffer;

                    try
                    {
                        if (result.IsCanceled)
                        {
                            break;
                        }

                        if (!buffer.IsEmpty)
                        {
                            try
                            {
                                if (WebSocketCanSend(socket))
                                {
                                    await socket.SendAsync(buffer, WebSocketMessageType.Text);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (Exception)
                            {
                                break;
                            }
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        _application.Input.AdvanceTo(buffer.End);
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                if (WebSocketCanSend(socket))
                {
                    try
                    {
                        await socket.CloseOutputAsync(
                            error != null ? WebSocketCloseStatus.InternalServerError : WebSocketCloseStatus.NormalClosure,
                            "",
                            CancellationToken.None);
                    }
                    catch (Exception)
                    {
                    }
                }

                _application!.Input.Complete();
            }
        }

        private static bool WebSocketCanSend(WebSocket ws)
        {
            return !(ws.State == WebSocketState.Aborted ||
                   ws.State == WebSocketState.Closed ||
                   ws.State == WebSocketState.CloseSent);
        }

        public async Task StopAsync()
        {
            if (_application == null)
            {
                return;
            }

            _transport!.Output.Complete();
            _transport!.Input.Complete();

            _application.Input.CancelPendingRead();

            try
            {
                await Running;
            }
            catch (Exception)
            {
                return;
            }
            finally
            {
                _webSocket?.Dispose();
            }
        }
    }
}
