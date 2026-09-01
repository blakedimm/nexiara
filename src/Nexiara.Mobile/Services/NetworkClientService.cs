using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Nexiara.Mobile.Services
{
    public class NetworkClientService
    {
        private static NetworkClientService? _instance;
        public static NetworkClientService Instance => _instance ??= new NetworkClientService();

        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cts;

        public event Action<string>? MessageReceived;

        public bool IsConnected => _webSocket?.State == WebSocketState.Open;

        public async Task<bool> ConnectAsync(string ipAddress, int port = 5000)
        {
            try
            {
                Disconnect();
                _webSocket = new ClientWebSocket();
                _cts = new CancellationTokenSource();

                var uri = new Uri($"ws://{ipAddress}:{port}/");
                await _webSocket.ConnectAsync(uri, _cts.Token);

                // Запускаем слушатель входящих данных от ПК
                _ = ListenAsync(_cts.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task ListenAsync(CancellationToken token)
        {
            var buffer = new byte[4096];
            while (!token.IsCancellationRequested && IsConnected)
            {
                try
                {
                    var result = await _webSocket!.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    MessageReceived?.Invoke(message);
                }
                catch { break; }
            }
        }

        public async Task SendActionAsync(object payload)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open) return;

            try
            {
                string json = JsonSerializer.Serialize(payload);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch { }
        }

        public void Disconnect()
        {
            try
            {
                _cts?.Cancel();
                _webSocket?.Dispose();
                _webSocket = null;
            }
            catch { }
        }
    }
}