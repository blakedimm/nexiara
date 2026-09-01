using System;
using System.Threading.Tasks;
using Nexiara.Core.Dispatcher;
using Nexiara.Core.Network;
using Nexiara.Core.Security;
using Nexiara.Core.State;
using Nexiara.Protocol;

namespace Nexiara.Core
{
    /// <summary>
    /// Единый управляющий фасад меш-узла Nexiara.
    /// </summary>
    public class MeshNode
    {
        public NodeHandshake LocalHandshake { get; }
        public ActionDispatcher Dispatcher { get; }
        public NodeRegistry Registry { get; }
        public TrustManager Trust { get; }
        public UdpDiscoveryService Discovery { get; }
        public TcpNodeListener Listener { get; }
        public TcpNodeClient Client { get; }

        public MeshNode(NodeHandshake localHandshake)
        {
            LocalHandshake = localHandshake;
            Dispatcher = new ActionDispatcher();
            Registry = new NodeRegistry();
            Trust = new TrustManager();
            Discovery = new UdpDiscoveryService(Registry, LocalHandshake);
            Listener = new TcpNodeListener(Dispatcher);
            Client = new TcpNodeClient(Dispatcher);
        }

        /// <summary>
        /// Запускает сетевое ядро (UDP авто-поиск и TCP сервер подслушивания).
        /// </summary>
        public void Start()
        {
            Listener.Start();
            Discovery.Start();
        }

        /// <summary>
        /// Отправляет действие на конкретную подключенную ноду.
        /// </summary>
        public async Task<bool> SendActionAsync<T>(string targetNodeId, T action) where T : INexiaraAction
        {
            if (Listener.ActiveSessions.TryGetValue(targetNodeId, out var session))
            {
                var envelope = ActionEnvelope.Create(action, LocalHandshake.NodeId);
                await session.SendEnvelopeAsync(envelope);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Останавливает все сетевые службы.
        /// </summary>
        public void Stop()
        {
            Discovery.Stop();
            Listener.Stop();
        }
    }
}