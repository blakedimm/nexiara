using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nexiara.Core.Dispatcher;
using Nexiara.Protocol;
using Nexiara.Protocol.Serialization;

namespace Nexiara.Core.Network
{
    public class PeerSession
    {
        private readonly TcpClient _client;
        private readonly ActionDispatcher _dispatcher;
        private readonly NetworkStream _stream;
        private CancellationTokenSource? _cts;

        public string RemoteNodeId { get; private set; } = string.Empty;
        public bool IsConnected => _client.Connected;

        public event Action<PeerSession>? OnDisconnected;

        public PeerSession(TcpClient client, ActionDispatcher dispatcher)
        {
            _client = client;
            _client.NoDelay = true; // Мгновенная отправка без задержки Нагла
            _dispatcher = dispatcher;
            _stream = _client.GetStream();
        }

        public void StartListening()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => ReadLoopAsync(_cts.Token));
        }

        /// <summary>
        /// Отправляет сетевой конверт с фреймингом длины.
        /// </summary>
        public async Task SendEnvelopeAsync(ActionEnvelope envelope)
        {
            if (!IsConnected) return;

            try
            {
                string json = JsonSerializer.Serialize(envelope, ActionJsonContext.Default.ActionEnvelope);
                byte[] payload = Encoding.UTF8.GetBytes(json);
                byte[] lengthBuffer = BitConverter.GetBytes(payload.Length);

                // Отправляем 4 байта длины, а затем сам JSON
                await _stream.WriteAsync(lengthBuffer, 0, 4);
                await _stream.WriteAsync(payload, 0, payload.Length);
                await _stream.FlushAsync();
            }
            catch
            {
                Disconnect();
            }
        }

        private async Task ReadLoopAsync(CancellationToken token)
        {
            var lengthBuffer = new byte[4];

            try
            {
                while (!token.IsCancellationRequested && _client.Connected)
                {
                    // 1. Читаем размер следующего пакета (4 байта)
                    int bytesRead = await ReadExactAsync(_stream, lengthBuffer, 4, token);
                    if (bytesRead < 4) break;

                    int packetLength = BitConverter.ToInt32(lengthBuffer, 0);
                    if (packetLength <= 0 || packetLength > 20 * 1024 * 1024) break; // Защита от мусора (>20MB)

                    // 2. Читаем сам JSON буфер
                    var payloadBuffer = new byte[packetLength];
                    bytesRead = await ReadExactAsync(_stream, payloadBuffer, packetLength, token);
                    if (bytesRead < packetLength) break;

                    // 3. Десериализуем и отсылаем в ActionDispatcher
                    string json = Encoding.UTF8.GetString(payloadBuffer);
                    var envelope = JsonSerializer.Deserialize<ActionEnvelope>(json, ActionJsonContext.Default.ActionEnvelope);

                    if (envelope != null)
                    {
                        RemoteNodeId = envelope.SenderNodeId;
                        await _dispatcher.DispatchAsync(envelope);
                    }
                }
            }
            catch { }
            finally
            {
                Disconnect();
            }
        }

        private async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int count, CancellationToken token)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, totalRead, count - totalRead, token);
                if (read == 0) return totalRead;
                totalRead += read;
            }
            return totalRead;
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            _client.Close();
            OnDisconnected?.Invoke(this);
        }
    }
}