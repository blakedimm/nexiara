using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nexiara.Core.State;
using Nexiara.Protocol;
using Nexiara.Protocol.Serialization;

namespace Nexiara.Core.Network
{
    public class UdpDiscoveryService
    {
        private const int DiscoveryPort = 8888;
        private readonly NodeRegistry _nodeRegistry;
        private readonly NodeHandshake _localNodeHandshake;
        private UdpClient? _udpClient;
        private CancellationTokenSource? _cts;

        public UdpDiscoveryService(NodeRegistry nodeRegistry, NodeHandshake localNodeHandshake)
        {
            _nodeRegistry = nodeRegistry;
            _localNodeHandshake = localNodeHandshake;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _udpClient = new UdpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

            // Фоновое слушание и фоновое вещание
            Task.Run(() => ListenAsync(_cts.Token));
            Task.Run(() => BroadcastAsync(_cts.Token));
        }

        private async Task BroadcastAsync(CancellationToken token)
        {
            var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    string json = JsonSerializer.Serialize(_localNodeHandshake, ActionJsonContext.Default.NodeHandshake);
                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    await _udpClient!.SendAsync(bytes, bytes.Length, broadcastEndpoint);
                }
                catch { }

                await Task.Delay(3000, token); // Вещаем раз в 3 секунды
            }
        }

        private async Task ListenAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient!.ReceiveAsync(token);
                    string json = Encoding.UTF8.GetString(result.Buffer);
                    var remoteHandshake = JsonSerializer.Deserialize<NodeHandshake>(json, ActionJsonContext.Default.NodeHandshake);

                    // Игнорируем собственные сигналы
                    if (remoteHandshake != null && remoteHandshake.NodeId != _localNodeHandshake.NodeId)
                    {
                        _nodeRegistry.RegisterOrUpdateNode(remoteHandshake);
                    }
                }
                catch { }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _udpClient?.Close();
        }
    }
}