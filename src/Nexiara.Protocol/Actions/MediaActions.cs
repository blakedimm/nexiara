namespace Nexiara.Protocol.Actions
{
    /// <summary>
    /// Запрос на создание скриншотa указанного монитора.
    /// </summary>
    public record TakeScreenshotAction(int DisplayIndex = 0, bool FullQuality = false) : INexiaraAction
    {
        public string ActionType => "media.take_screenshot";
    }

    /// <summary>
    /// Передача данных изображения (ответа на запрос скриншота).
    /// </summary>
    public record ScreenshotResponseAction(byte[] ImageData, string Format = "png") : INexiaraAction
    {
        public string ActionType => "media.screenshot_response";
    }

    /// <summary>
    /// Потоковый видеокадр трансляции экрана (для режима управления).
    /// </summary>
    public record StreamFrameAction(byte[] FrameData, int Width, int Height, long FrameNumber) : INexiaraAction
    {
        public string ActionType => "media.stream_frame";
    }

    /// <summary>
    /// Передача фрагмента аудиопотока (Loopback/Микрофон).
    /// </summary>
    public record PlayAudioChunkAction(byte[] AudioData, int SampleRate = 44100, int Channels = 2) : INexiaraAction
    {
        public string ActionType => "media.play_audio_chunk";
    }
}