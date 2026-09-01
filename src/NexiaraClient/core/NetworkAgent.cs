using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NexiaraClient.Core
{
    public class NetworkAgent
    {
        private ClientWebSocket _webSocket;
        private readonly string _serverIp;
        private readonly string _deviceUuid;
        private CancellationTokenSource _cts;

        public NetworkAgent(string serverIp, string deviceUuid)
        {
            _serverIp = serverIp;
            _deviceUuid = deviceUuid;
        }

        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();

            bool isLocal = _serverIp.Contains("127.0.0.1") || _serverIp.Contains("localhost");
            string protocol = isLocal ? "ws" : "wss";
            string port = isLocal ? ":8000" : "";
            string uri = $"{protocol}://{_serverIp}{port}/api/touchpad/stream/register_agent/{_deviceUuid}";

            while (!_cts.Token.IsCancellationRequested)
            {
                using (_webSocket = new ClientWebSocket())
                {
                    try
                    {
                        Console.WriteLine($"[NEXIARA AGENT] Подключение к {uri}...");
                        await _webSocket.ConnectAsync(new Uri(uri), _cts.Token);
                        Console.WriteLine("[NEXIARA AGENT] Канал управления успешно установлен.");

                        var buffer = new byte[1024 * 4];
                        while (_webSocket.State == WebSocketState.Open)
                        {
                            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                            if (result.MessageType == WebSocketMessageType.Close) break;

                            string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            ProcessCommand(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[NEXIARA AGENT] Сбой соединения: {ex.Message}. Реконнект через 3с...");
                        await Task.Delay(3000, _cts.Token);
                    }
                }
            }
        }

        private void ProcessCommand(string jsonMsg)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(jsonMsg);
                JsonElement root = doc.RootElement;
                if (!root.TryGetProperty("action", out JsonElement actionEl)) return;

                string action = actionEl.GetString();

                if (action == "move")
                {
                    InputSimulator.MoveMouse(root.GetProperty("dx").GetInt32(), root.GetProperty("dy").GetInt32());
                }
                else if (action == "click")
                {
                    InputSimulator.Click(root.GetProperty("button").GetString());
                }
                else if (action == "scroll")
                {
                    InputSimulator.Scroll(root.GetProperty("dy").GetInt32());
                }
                else if (action == "set_volume")
                {
                    AudioController.SetVolume(root.GetProperty("level").GetInt32());
                }
                else if (action == "press_key")
                {
                    string keyName = root.GetProperty("key").GetString()?.ToLower();
                    byte vkCode = 0;

                    if (keyName == "enter") vkCode = 0x0D;
                    else if (keyName == "esc") vkCode = 0x1B;
                    else if (keyName == "backspace") vkCode = 0x08;
                    else if (keyName == "left") vkCode = 0x25;
                    else if (keyName == "up") vkCode = 0x26;
                    else if (keyName == "right") vkCode = 0x27;
                    else if (keyName == "down") vkCode = 0x28;
                    else if (keyName.Length == 1)
                    {
                        char c = keyName.ToUpper()[0];
                        vkCode = (byte)c;
                    }

                    if (vkCode != 0) InputSimulator.PressKey(vkCode);
                }
                else if (action == "disconnect")
                {
                    Stop();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NEXIARA AGENT] Ошибка парсинга команды: {ex.Message}");
            }
        }

        public void Stop() => _cts?.Cancel();
    }
}