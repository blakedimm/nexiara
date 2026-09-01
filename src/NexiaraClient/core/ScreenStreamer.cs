using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NexiaraClient.Core
{
    public class ScreenStreamer
    {
        private ClientWebSocket _webSocket;
        private readonly string _serverIp;
        private readonly string _deviceUuid;
        private readonly int _targetWidth;
        private readonly int _targetHeight;
        private readonly bool _hwAccel;
        private CancellationTokenSource _cts;

        // Обновленный конструктор с параметрами из UI
        public ScreenStreamer(string serverIp, string deviceUuid, int targetWidth, int targetHeight, bool hwAccel)
        {
            _serverIp = serverIp;
            _deviceUuid = deviceUuid;
            _targetWidth = targetWidth;
            _targetHeight = targetHeight;
            _hwAccel = hwAccel;
        }

        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            string uri = $"ws://{_serverIp}:8000/api/touchpad/stream/register_agent/{_deviceUuid}";

            while (!_cts.Token.IsCancellationRequested)
            {
                using (_webSocket = new ClientWebSocket())
                {
                    try
                    {
                        await _webSocket.ConnectAsync(new Uri(uri), _cts.Token);
                        var buffer = new byte[128];
                        while (_webSocket.State == WebSocketState.Open)
                        {
                            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                            string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            
                            if (msg.Contains("pull_frame"))
                            {
                                await SendFrameAsync();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        await Task.Delay(2000, _cts.Token);
                    }
                }
            }
        }

        private async Task SendFrameAsync()
        {
            try
            {
                // Для мультимониторных систем берем основной экран
                int screenW = 1920;
                int screenH = 1080;

                using Bitmap bmp = new Bitmap(screenW, screenH);
                using Graphics g = Graphics.FromImage(bmp);
                g.CopyFromScreen(0, 0, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);

                using Bitmap resized = new Bitmap(bmp, new Size(_targetWidth, _targetHeight));
                using MemoryStream ms = new MemoryStream();

                ImageCodecInfo? jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                EncoderParameters encoderParameters = new EncoderParameters(1);

                long quality = _hwAccel ? 50L : 70L;

                // 🔥 ИСПРАВЛЕНИЕ: Явно указываем пространство имен для Encoder
                encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

                if (jpegEncoder != null)
                {
                    resized.Save(ms, jpegEncoder, encoderParameters);
                }

                var segment = new ArraySegment<byte>(ms.ToArray());
                await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, _cts.Token);
            }
            catch { /* Игнорируем ошибки кадра для плавности */ }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageEncoders())
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            return null;
        }

        public void Stop() => _cts?.Cancel();
    }
}