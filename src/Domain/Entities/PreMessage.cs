using Pgvector;

namespace ChatbotApi.Domain.Entities;

public class PreMessage
{
    public Chatbot ChatBot { get; set; } = default!;
    public int ChatBotId { get; set; }
    public int Order { get; set; }
    public string UserMessage { get; set; } = default!;
    public string? AssistantMessage { get; set; } = default!;
    public bool IsRequired { get; set; }
    public string? FileName { get; set; }
    public string? FileHash { get; set; }
    public Vector? Embedding { get; set; }

    public byte[]? FileContent { get; set; }
    public string? FileMimeType { get; set; }
    public bool? UseCag { get; set; } = false;
    public string? Url { get; set; }
    public string? CronJob { get; set; }
    public int ChunkSize { get; set; }
    public int OverlappingSize { get; set; }
    public DateTime? LastUpdate { get; set; }

    public PreMessageContent? PreMessageContent { get; set; }
    public int? PreMessageContentId { get; set; }
}
