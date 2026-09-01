using System;
using System.Text.Json;

namespace Nexiara.Protocol
{
    /// <summary>
    /// Базовый интерфейс для всех строго типизированных действий в сети Nexiara.
    /// </summary>
    public interface INexiaraAction
    {
        /// <summary>
        /// Уникальный строковый идентификатор типа действия (например, "mouse.move").
        /// </summary>
        string ActionType { get; }
    }

    /// <summary>
    /// Сетевой конверт для упаковки, передачи по TCP и маршрутизации любых действий.
    /// </summary>
    public class ActionEnvelope
    {
        public string ActionType { get; set; } = string.Empty;
        public string SenderNodeId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
        public string PayloadJson { get; set; } = string.Empty;
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>
        /// Упаковывает строго типизированный Action в сетевой конверт.
        /// </summary>
        public static ActionEnvelope Create<T>(T action, string senderNodeId) where T : INexiaraAction
        {
            return new ActionEnvelope
            {
                ActionType = action.ActionType,
                SenderNodeId = senderNodeId,
                PayloadJson = JsonSerializer.Serialize(action)
            };
        }
    }
}