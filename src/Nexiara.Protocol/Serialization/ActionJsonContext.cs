using System.Text.Json.Serialization;
using Nexiara.Protocol.Actions;

namespace Nexiara.Protocol.Serialization
{
    /// <summary>
    /// Оптимизированный контекст генерации JSON-кода для асинхронной сериализации.
    /// </summary>
    [JsonSourceGenerationOptions(
        WriteIndented = false,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(NodeHandshake))]
    [JsonSerializable(typeof(ActionEnvelope))]
    [JsonSerializable(typeof(MoveMouseAction))]
    [JsonSerializable(typeof(MouseClickAction))]
    [JsonSerializable(typeof(MouseScrollAction))]
    [JsonSerializable(typeof(KeyPressAction))]
    [JsonSerializable(typeof(TouchpadGestureAction))]
    [JsonSerializable(typeof(SetVolumeAction))]
    [JsonSerializable(typeof(RunAppAction))]
    [JsonSerializable(typeof(LockPcAction))]
    [JsonSerializable(typeof(SystemPowerAction))]
    [JsonSerializable(typeof(GetProcessesRequestAction))]
    [JsonSerializable(typeof(KillProcessAction))]
    [JsonSerializable(typeof(TakeScreenshotAction))]
    [JsonSerializable(typeof(ScreenshotResponseAction))]
    [JsonSerializable(typeof(TypeTextAction))]
    [JsonSerializable(typeof(StreamFrameAction))]
    [JsonSerializable(typeof(PlayAudioChunkAction))]
    [JsonSerializable(typeof(CopyClipboardAction))]
    [JsonSerializable(typeof(SyncProjectAction))]
    [JsonSerializable(typeof(SendFileChunkAction))]
    [JsonSerializable(typeof(FileTransferRequestAction))]
    public partial class ActionJsonContext : JsonSerializerContext
    {
        // Генератор кода сам подставит конструкторы и статическое свойство .Default
    }
}