using System;
using System.IO;
using System.Windows;
using Nexiara.Core;
using Nexiara.Core.Network;
using Nexiara.Platform.Windows.Executors;
using Nexiara.Protocol;
using Nexiara.Protocol.Actions;
using Nexiara.Streaming.Models;
using Nexiara.Streaming.Pipeline;

namespace Nexiara.UI
{
    public partial class App : System.Windows.Application
    {
        public static MeshNode Node { get; private set; } = null!;
        public static StreamPipeline Pipeline { get; private set; } = null!;
        public static WebBridgeServer WebBridge { get; private set; } = null!;
        public static UdpBeaconListener Beacon { get; private set; } = null!;

        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => LogCrash(e.ExceptionObject as Exception);

            var app = new App();
            app.Run();
        }

        protected override async void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var handshake = new NodeHandshake
                {
                    NodeId = Environment.MachineName,
                    Platform = "Windows",
                    Capabilities = new()
                    {
                        NodeCapabilities.InputMouse,
                        NodeCapabilities.InputKeyboard,
                        NodeCapabilities.SystemWin32,
                        NodeCapabilities.ScreenCapture,
                        NodeCapabilities.SystemClipboard
                    }
                };

                Node = new MeshNode(handshake);

                var inputExecutor = new Win32InputExecutor();
                var systemExecutor = new Win32SystemExecutor();

                Node.Dispatcher.RegisterHandler<MoveMouseAction>("input.mouse_move", async (act, sender) => await inputExecutor.MoveMouseAsync(act));
                Node.Dispatcher.RegisterHandler<MouseClickAction>("input.mouse_click", async (act, sender) => await inputExecutor.ClickMouseAsync(act));
                Node.Dispatcher.RegisterHandler<MouseScrollAction>("input.mouse_scroll", async (act, sender) => await inputExecutor.ScrollMouseAsync(act));
                Node.Dispatcher.RegisterHandler<KeyPressAction>("input.key_press", async (act, sender) => await inputExecutor.SendKeyPressAsync(act));
                Node.Dispatcher.RegisterHandler<TypeTextAction>("input.type_text", async (act, sender) => await inputExecutor.TypeTextAsync(act));
                Node.Dispatcher.RegisterHandler<LockPcAction>("system.lock_pc", async (act, sender) => await systemExecutor.LockPcAsync(act));

                Node.Start();

                // 🔥 Запуск нового Lock-Free StreamPipeline (WGC -> TripleBuffer -> MediaFoundation -> WebSocket) 🔥
                try
                {
                    Pipeline = new StreamPipeline();
                    await Pipeline.StartAsync(width: 1280, height: 720, fps: 30);
                }
                catch (Exception ex)
                {
                    LogCrash(new Exception("Ошибка запуска StreamPipeline: " + ex.Message, ex));
                }

                WebBridge = new WebBridgeServer(Node, 5000);
                WebBridge.OnAbsoluteTouch += (nx, ny, action) =>
                {
                    Pipeline?.ProcessTouch(new TouchInput(nx, ny, (TouchAction)action));
                };
                WebBridge.Start();

                Beacon = new UdpBeaconListener(5001);
                Beacon.Start();

                var overlay = new MascotOverlayWindow();
                overlay.Show();
            }
            catch (Exception ex)
            {
                LogCrash(ex);
                MessageBox.Show($"Ошибка запуска приложения:\n{ex.Message}", "Nexiara Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private static void LogCrash(Exception? ex)
        {
            if (ex == null) return;
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
                File.AppendAllText(logPath, $"[{DateTime.Now}] CRASH: {ex}\n----------------------------------\n");
            }
            catch { }
        }

        protected override void OnExit(System.Windows.ExitEventArgs e)
        {
            Beacon?.Stop();
            WebBridge?.Stop();
            Pipeline?.Stop();
            Node?.Stop();
            base.OnExit(e);
        }
    }
}