using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Nexiara.Streaming.Transport
{
    public class StreamServer
    {
        private HttpListener? _listener;
        private CancellationTokenSource? _cts;
        private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();

        // 🔥 Обертка, чтобы Interlocked имел прямой доступ к памяти переменной (ref)
        private class ClientState
        {
            public int IsSending = 0;
        }

        private readonly ConcurrentDictionary<Guid, ClientState> _clientStates = new();

        public void Start(int port = 5005)
        {
            _cts = new CancellationTokenSource();
            _listener = new HttpListener();

            _listener.Prefixes.Add($"http://+:{port}/stream/");

            try
            {
                _listener.Start();
                Task.Run(() => AcceptConnectionsAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StreamServer Error] Ошибка запуска: {ex.Message}");
            }
        }

        private async Task AcceptConnectionsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener!.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        var wsContext = await context.AcceptWebSocketAsync(null);
                        var clientId = Guid.NewGuid();

                        _clients.TryAdd(clientId, wsContext.WebSocket);
                        _clientStates.TryAdd(clientId, new ClientState());

                        _ = Task.Run(() => HandleClientAsync(clientId, wsContext.WebSocket, token));
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
                catch { }
            }
        }

        private async Task HandleClientAsync(Guid clientId, WebSocket ws, CancellationToken token)
        {
            var buffer = new byte[1024];
            try
            {
                while (ws.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close) break;
                }
            }
            catch { }
            finally
            {
                _clients.TryRemove(clientId, out _);
                _clientStates.TryRemove(clientId, out _);
                if (ws.State == WebSocketState.Open)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                ws.Dispose();
            }
        }

        public void BroadcastFrame(byte[] frameData)
        {
            foreach (var kvp in _clients)
            {
                var clientId = kvp.Key;
                var client = kvp.Value;

                if (client.State == WebSocketState.Open && _clientStates.TryGetValue(clientId, out var state))
                {
                    // 🔥 Теперь передаем поле класса по ссылке (ref state.IsSending)
                    if (Interlocked.CompareExchange(ref state.IsSending, 1, 0) == 0)
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                await client.SendAsync(new ArraySegment<byte>(frameData), WebSocketMessageType.Binary, true, CancellationToken.None);
                            }
                            catch { }
                            finally
                            {
                                // Отпускаем блокировку
                                state.IsSending = 0;
                            }
                        });
                    }
                }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            foreach (var ws in _clients.Values) ws.Dispose();
            _clients.Clear();
            _clientStates.Clear();
        }
    }
}