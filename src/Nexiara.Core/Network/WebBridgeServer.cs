using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nexiara.Protocol;
using Nexiara.Protocol.Actions;

namespace Nexiara.Core.Network
{
    public class WebBridgeServer
    {
        private readonly HttpListener _listener = new();
        private readonly MeshNode _node;
        private readonly int _port;
        private CancellationTokenSource? _cts;

        public event Action<float, float, int>? OnAbsoluteTouch;

        public WebBridgeServer(MeshNode node, int port = 5000)
        {
            _node = node;
            _port = port;
            _listener.Prefixes.Add($"http://*:{_port}/");
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            try
            {
                _listener.Start();
                Task.Run(() => ListenAsync(_cts.Token));
            }
            catch { }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener.Stop();
        }

        private async Task ListenAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync();

                    if (context.Request.IsWebSocketRequest)
                    {
                        _ = ProcessWebSocketAsync(context);
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

        private async Task ProcessWebSocketAsync(HttpListenerContext context)
        {
            var wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
            var webSocket = wsContext.WebSocket;
            byte[] buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) break;

                string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    string type = root.GetProperty("type").GetString() ?? "";

                    // 🔥 НОВЫЙ РОУТИНГ КОМАНД С МОБИЛКИ 🔥
                    switch (type)
                    {
                        case "move":
                            float dx = root.GetProperty("dx").GetSingle();
                            float dy = root.GetProperty("dy").GetSingle();
                            await _node.Dispatcher.DispatchAsync(ActionEnvelope.Create(new MoveMouseAction(dx, dy), "Mobile"));
                            break;

                        case "click":
                            string btn = root.GetProperty("button").GetString() ?? "left";
                            await _node.Dispatcher.DispatchAsync(ActionEnvelope.Create(new MouseClickAction(btn), "Mobile"));
                            break;

                        case "scroll":
                            int scrollDy = root.GetProperty("dy").GetInt32();
                            // Умножаем на 120 (стандартный шаг колесика в Windows)
                            await _node.Dispatcher.DispatchAsync(ActionEnvelope.Create(new MouseScrollAction(0, scrollDy * 120), "Mobile"));
                            break;

                        case "key_combo":
                            string combo = root.GetProperty("combo").GetString() ?? "";
                            if (combo == "alt_tab")
                                await _node.Dispatcher.DispatchAsync(ActionEnvelope.Create(new KeyPressAction("tab", Alt: true), "Mobile"));
                            else if (combo == "ctrl_shift_esc")
                                await _node.Dispatcher.DispatchAsync(ActionEnvelope.Create(new KeyPressAction("esc", Control: true, Shift: true), "Mobile"));
                            else
                                await _node.Dispatcher.DispatchAsync(ActionEnvelope.Create(new KeyPressAction(combo), "Mobile"));
                            break;

                        case "type_text":
                            string txt = root.GetProperty("text").GetString() ?? "";
                            await _node.Dispatcher.DispatchAsync(ActionEnvelope.Create(new TypeTextAction(txt), "Mobile"));
                            break;

                        case "lock":
                            await _node.Dispatcher.DispatchAsync(ActionEnvelope.Create(new LockPcAction(), "Mobile"));
                            break;

                        case "abs_touch":
                            float nx = root.GetProperty("x").GetSingle();
                            float ny = root.GetProperty("y").GetSingle();
                            int action = root.GetProperty("action").GetInt32();
                            OnAbsoluteTouch?.Invoke(nx, ny, action);
                            break;
                    }
                }
                catch { }
            }
        }
    }
}