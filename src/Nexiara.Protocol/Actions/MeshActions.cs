namespace Nexiara.Protocol.Actions
{
    /// <summary>
    /// Синхронизация текста или ссылки в системный буфер обмена.
    /// </summary>
    public record CopyClipboardAction(string Text, bool IsHtml = false) : INexiaraAction
    {
        public string ActionType => "mesh.copy_clipboard";
    }

    /// <summary>
    /// Запрос на синхронизацию рабочего проекта (Git/Zip).
    /// </summary>
    public record SyncProjectAction(string ProjectName, string TargetDirectory, string ArchiveUrlOrPath) : INexiaraAction
    {
        public string ActionType => "mesh.sync_project";
    }

    /// <summary>
    /// Передача одного чанка (куска) файла для фоновой загрузки.
    /// </summary>
    public record SendFileChunkAction(string TransferId, string RelativePath, long ChunkIndex, long TotalChunks, byte[] Data) : INexiaraAction
    {
        public string ActionType => "mesh.send_file_chunk";
    }

    /// <summary>
    /// Запрос на передачу файла с одного узла на другой.
    /// </summary>
    public record FileTransferRequestAction(string SourcePath, string DestinationPath) : INexiaraAction
    {
        public string ActionType => "mesh.file_request";
    }
}