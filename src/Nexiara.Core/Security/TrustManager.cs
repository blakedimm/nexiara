using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace Nexiara.Core.Security
{
    /// <summary>
    /// Менеджер доверенных устройств и токенов авторизации.
    /// </summary>
    public class TrustManager
    {
        private readonly ConcurrentDictionary<string, string> _trustedNodes = new();
        private readonly string _storagePath;

        public TrustManager(string storagePath = "trusted_nodes.json")
        {
            _storagePath = storagePath;
            Load();
        }

        public bool IsTrusted(string nodeId)
        {
            return _trustedNodes.ContainsKey(nodeId);
        }

        public void AddTrustedNode(string nodeId, string trustToken)
        {
            _trustedNodes[nodeId] = trustToken;
            Save();
        }

        public void RemoveTrustedNode(string nodeId)
        {
            _trustedNodes.TryRemove(nodeId, out _);
            Save();
        }

        private void Load()
        {
            if (File.Exists(_storagePath))
            {
                try
                {
                    string json = File.ReadAllText(_storagePath);
                    var dict = JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(json);
                    if (dict != null)
                    {
                        foreach (var kvp in dict) _trustedNodes[kvp.Key] = kvp.Value;
                    }
                }
                catch { }
            }
        }

        private void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(_trustedNodes);
                File.WriteAllText(_storagePath, json);
            }
            catch { }
        }
    }
}