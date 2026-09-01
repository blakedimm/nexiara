#nullable disable
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SIPSorcery.Net;
using SIPSorceryMedia.Windows;
using SIPSorceryMedia.Encoders;

namespace NexiaraClient.Core
{
    public class WebRtcStreamer
    {
        private ClientWebSocket _signalingSocket;
        private RTCPeerConnection _peerConnection;
        private WindowsVideoEndPoint _videoEndPoint;

        private readonly string _serverIp;
        private readonly string _deviceUuid;
        private readonly string _apiKey;
        private CancellationTokenSource _cts;

        public WebRtcStreamer(string serverIp, string deviceUuid, string apiKey = "")
        {
            _serverIp = serverIp;
            _deviceUuid = deviceUuid;
            _apiKey = apiKey;
        }

        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();

            // Отключаем внутренний спам-логгинг SIPSorcery в консоль
            SIPSorcery.LogFactory.Set(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

            // 1. Инициализируем VP8 кодек и Windows-видеозахват
            var videoEncoder = new VpxVideoEncoder();
            _videoEndPoint = new WindowsVideoEndPoint(videoEncoder, null, 1280U, 720U, 30U);

            // 2. Нативно создаем видео-трек из поддерживаемых форматов энкодера
            var videoTrack = new MediaStreamTrack(_videoEndPoint.GetVideoSourceFormats(), MediaStreamStatusEnum.SendOnly);

            _peerConnection = new RTCPeerConnection(null);
            _peerConnection.addTrack(videoTrack);

            _peerConnection.OnVideoFormatsNegotiated += (negotiatedFormats) => _videoEndPoint.SetVideoSourceFormat(negotiatedFormats[0]);

            _peerConnection.onconnectionstatechange += (state) =>
            {
                Console.WriteLine($"[WebRTC] Статус: {state}");
                if (state == RTCPeerConnectionState.connected)
                {
                    _videoEndPoint.StartVideo();
                }
                else if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed)
                {
                    _videoEndPoint.CloseVideo();
                }
            };

            _peerConnection.onicecandidate += (candidate) =>
            {
                if (candidate != null && _signalingSocket?.State == WebSocketState.Open)
                {
                    var msg = new { type = "ice_candidate", candidate = candidate.toJSON() };
                    _ = SendSignalingMsgAsync(msg);
                }
            };

            // 3. Коннектимся к сигнальному хабу на бэкенде
            await ConnectSignalingAsync();
        }

        private async Task ConnectSignalingAsync()
        {
            // Определяем локалку или публичный туннель
            bool isLocal = _serverIp.Contains("localhost") || _serverIp.Contains("127.0.0.1");
            string wsProtocol = isLocal ? "ws" : "wss";
            string port = isLocal ? ":8000" : "";

            // Формируем правильный URI без порта :8000 для внешних доменов
            string uri = $"{wsProtocol}://{_serverIp}{port}/api/webrtc/signaling/{_deviceUuid}/agent?api_key={_apiKey}";

            using (_signalingSocket = new ClientWebSocket())
            {
                try
                {
                    Console.WriteLine($"[WebRTC] Подключение к сигналке: {uri}");
                    await _signalingSocket.ConnectAsync(new Uri(uri), _cts.Token);
                    Console.WriteLine("[WebRTC] Сигнальный канал успешно установлен.");

                    var buffer = new byte[8192];
                    while (_signalingSocket.State == WebSocketState.Open)
                    {
                        var result = await _signalingSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close) break;

                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        ProcessSignalingMessage(message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WebRTC] Обрыв сигнального канала: {ex.Message}");
                }
            }
        }

        private void ProcessSignalingMessage(string jsonMsg)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(jsonMsg);
                JsonElement root = doc.RootElement;
                if (!root.TryGetProperty("type", out JsonElement typeEl)) return;

                string type = typeEl.GetString();

                if (type == "offer")
                {
                    var result = RTCSessionDescriptionInit.TryParse(jsonMsg, out var offer);
                    if (result)
                    {
                        var setOfferResult = _peerConnection.setRemoteDescription(offer);
                        if (setOfferResult == SetDescriptionResultEnum.OK)
                        {
                            var answer = _peerConnection.createAnswer(null);
                            _peerConnection.setLocalDescription(answer);

                            _ = SendSignalingMsgAsync(new { type = "answer", sdp = answer.sdp });
                        }
                    }
                }
                else if (type == "ice_candidate")
                {
                    // Безопасный ручной парсинг кандидата, защищенный от любых изменений API SIPSorcery
                    if (root.TryGetProperty("candidate", out var candEl))
                    {
                        var init = new RTCIceCandidateInit();

                        if (candEl.ValueKind == JsonValueKind.Object)
                        {
                            if (candEl.TryGetProperty("candidate", out var cVal)) init.candidate = cVal.GetString();
                            if (candEl.TryGetProperty("sdpMid", out var mVal)) init.sdpMid = mVal.GetString();
                            if (candEl.TryGetProperty("sdpMLineIndex", out var iVal) && iVal.ValueKind != JsonValueKind.Null)
                            {
                                init.sdpMLineIndex = (ushort)iVal.GetUInt16();
                            }
                        }
                        else
                        {
                            init.candidate = candEl.GetString();
                        }

                        _peerConnection.addIceCandidate(init);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebRTC] Ошибка SDP: {ex.Message}");
            }
        }

        private async Task SendSignalingMsgAsync(object payload)
        {
            if (_signalingSocket != null && _signalingSocket.State == WebSocketState.Open)
            {
                string json = JsonSerializer.Serialize(payload);
                var bytes = Encoding.UTF8.GetBytes(json);
                await _signalingSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _videoEndPoint?.CloseVideo();
            _peerConnection?.Close("Остановлено пользователем");
        }
    }
}