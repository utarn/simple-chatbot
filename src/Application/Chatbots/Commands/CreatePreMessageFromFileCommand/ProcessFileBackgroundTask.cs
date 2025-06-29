namespace ChatbotApi.Application.Chatbots.Commands.CreatePreMessageFromFileCommand;

public class ProcessFileBackgroundTask
{
    public int ChatBotId { get; set; }
    public byte[]? FileContent { get; set; }
    public string FileName { get; set; } = null!;
    public string? FileMimeType { get; set; }
    public string? Url { get; set; }
    public int ChunkSize { get; set; }
    public int OverlappingSize { get; set; }
    public bool UseCag { get; set; }
    public bool IsRequired { get; set; }
    public string? CronJob { get; set; }
}
