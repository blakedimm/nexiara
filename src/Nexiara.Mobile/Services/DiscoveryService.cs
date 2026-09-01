using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Nexiara.Mobile.Services
{
    public class DiscoveredNode
    {
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
    }

    public class DiscoveryService
    {
        private const int BeaconPort = 5001;

        public async Task<DiscoveredNode?> FindPcAsync(int timeoutMs = 2500)
        {
            try
            {
                using var client = new UdpClient();
                client.EnableBroadcast = true;

                byte[] request = Encoding.UTF8.GetBytes("NEXIARA_DISCOVER_PING");
                var endPoint = new IPEndPoint(IPAddress.Broadcast, BeaconPort);

                await client.SendAsync(request, request.Length, endPoint);

                var receiveTask = client.ReceiveAsync();
                var delayTask = Task.Delay(timeoutMs);

                if (await Task.WhenAny(receiveTask, delayTask) == receiveTask)
                {
                    var result = receiveTask.Result;
                    string response = Encoding.UTF8.GetString(result.Buffer);

                    var parts = response.Split('|');
                    if (parts.Length >= 3 && parts[0] == "NEXIARA_PONG")
                    {
                        return new DiscoveredNode
                        {
                            Name = parts[1],
                            IpAddress = result.RemoteEndPoint.Address.ToString(),
                            Port = int.Parse(parts[2])
                        };
                    }
                }
            }
            catch
            {
                // Исключения сети обрабатываются возвратом null
            }

            return null;
        }
    }
}