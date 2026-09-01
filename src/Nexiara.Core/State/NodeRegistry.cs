using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Nexiara.Protocol;

namespace Nexiara.Core.State
{
    public class NodeRegistry
    {
        private readonly ConcurrentDictionary<string, (NodeHandshake Node, DateTime LastSeen)> _nodes = new();

        public event Action<NodeHandshake>? OnNodeDiscovered;
        public event Action<string>? OnNodeLost;

        /// <summary>
        /// Регистрирует или обновляет статус устройства в локальной сети.
        /// </summary>
        public void RegisterOrUpdateNode(NodeHandshake handshake)
        {
            bool isNew = !_nodes.ContainsKey(handshake.NodeId);
            _nodes[handshake.NodeId] = (handshake, DateTime.UtcNow);

            if (isNew)
            {
                OnNodeDiscovered?.Invoke(handshake);
            }
        }

        /// <summary>
        /// Возвращает список всех активных устройств в сети.
        /// </summary>
        public List<NodeHandshake> GetActiveNodes()
        {
            // Считаем ноду активной, если от неё был сигнал в последние 15 секунд
            var threshold = DateTime.UtcNow.AddSeconds(-15);
            return _nodes.Values
                .Where(x => x.LastSeen >= threshold)
                .Select(x => x.Node)
                .ToList();
        }

        /// <summary>
        /// Очищает устаревшие устройства из реестра.
        /// </summary>
        public void CleanupDeadNodes()
        {
            var threshold = DateTime.UtcNow.AddSeconds(-15);
            foreach (var key in _nodes.Keys)
            {
                if (_nodes.TryGetValue(key, out var entry) && entry.LastSeen < threshold)
                {
                    if (_nodes.TryRemove(key, out _))
                    {
                        OnNodeLost?.Invoke(key);
                    }
                }
            }
        }
    }
}