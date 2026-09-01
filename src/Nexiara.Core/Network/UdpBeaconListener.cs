using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nexiara.Core.Network
{
    public class UdpBeaconListener
    {
        private readonly int _port;
        private UdpClient? _udpServer;
        private CancellationTokenSource? _cts;

        public UdpBeaconListener(int port = 5001)
        {
            _port = port;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _udpServer = new UdpClient(_port);
            Task.Run(() => ListenAsync(_cts.Token));
        }

        private async Task ListenAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _udpServer != null)
            {
                try
                {
                    var result = await _udpServer.ReceiveAsync(token);
                    string message = Encoding.UTF8.GetString(result.Buffer);

                    if (message == "NEXIARA_DISCOVER_PING")
                    {
                        // Отвечаем телефону информацией о нашем ПК
                        string response = $"NEXIARA_PONG|{Environment.MachineName}|5000";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);

                        await _udpServer.SendAsync(responseBytes, responseBytes.Length, result.RemoteEndPoint);
                    }
                }
                catch { }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _udpServer?.Close();
        }
    }
}