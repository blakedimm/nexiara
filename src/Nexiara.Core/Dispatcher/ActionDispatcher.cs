using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using Nexiara.Protocol;

namespace Nexiara.Core.Dispatcher
{
    public class ActionDispatcher
    {
        private readonly ConcurrentDictionary<string, Func<string, string, Task>> _handlers = new();

        // Опции десериализации: игнорируем регистр символов (Dx vs dx)
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Регистрирует обработчик для конкретного типа действия TAction.
        /// </summary>
        public void RegisterHandler<TAction>(string actionType, Func<TAction, string, Task> handler) where TAction : INexiaraAction
        {
            _handlers[actionType] = async (payloadJson, senderNodeId) =>
            {
                var action = JsonSerializer.Deserialize<TAction>(payloadJson, _jsonOptions);
                if (action != null)
                {
                    await handler(action, senderNodeId);
                }
            };
        }

        /// <summary>
        /// Маршрутизирует пришедший из сети конверт к нужному исполнителю.
        /// </summary>
        public async Task DispatchAsync(ActionEnvelope envelope)
        {
            if (_handlers.TryGetValue(envelope.ActionType, out var handler))
            {
                await handler(envelope.PayloadJson, envelope.SenderNodeId);
            }
        }
    }
}