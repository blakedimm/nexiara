using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Nexiara.Core.Dispatcher;

namespace Nexiara.Core.Network
{
    public class TcpNodeClient
    {
        private readonly ActionDispatcher _dispatcher;

        public TcpNodeClient(ActionDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Подключается к целевому узлу по IP и образует рабочую сессию.
        /// </summary>
        public async Task<PeerSession?> ConnectAsync(string targetIp, int port = 8089)
        {
            try
            {
                var client = new TcpClient { NoDelay = true };
                await client.ConnectAsync(targetIp, port);

                var session = new PeerSession(client, _dispatcher);
                session.StartListening();
                return session;
            }
            catch
            {
                return null;
            }
        }
    }
}