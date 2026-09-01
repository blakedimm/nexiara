using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Nexiara.Core.Dispatcher;

namespace Nexiara.Core.Network
{
    public class TcpNodeListener
    {
        private const int DefaultTcpPort = 8089;
        private readonly ActionDispatcher _dispatcher;
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;

        public ConcurrentDictionary<string, PeerSession> ActiveSessions { get; } = new();

        public event Action<PeerSession>? OnPeerConnected;

        public TcpNodeListener(ActionDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void Start(int port = DefaultTcpPort)
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            Task.Run(() => AcceptLoopAsync(_cts.Token));
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener!.AcceptTcpClientAsync(token);
                    var session = new PeerSession(client, _dispatcher);

                    session.OnDisconnected += s =>
                    {
                        if (!string.IsNullOrEmpty(s.RemoteNodeId))
                            ActiveSessions.TryRemove(s.RemoteNodeId, out _);
                    };

                    session.StartListening();
                    OnPeerConnected?.Invoke(session);
                }
                catch { }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();

            foreach (var session in ActiveSessions.Values)
            {
                session.Disconnect();
            }
            ActiveSessions.Clear();
        }
    }
}