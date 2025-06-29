namespace ChatbotApi.Domain.Entities;

public class Error
{
    public int Id { get; set; }
    public string Type { get; set; }
    public DateTime Created { get; set; }
    public int ChatBotId { get; set; }
    public Chatbot ChatBot { get; set; }
    public string ExceptionMessage { get; set; } = null!;
    public bool IsDismissed { get; set; }


}


public class ImportError : Error
{
    public byte[]? FileContent { get; set; }
    public string FileName { get; set; } = null!;
    public string? FileMimeType { get; set; }
    public string? Url { get; set; }
    public int ChunkSize { get; set; }
    public int OverlappingSize { get; set; }
    public bool UseCag { get; set; }
    public bool IsRequired { get; set; }
}


public class RefreshInformation : Error
{
    public string FileName { get; set; } = null!;
    public string? Url { get; set; }
    public bool UseCag { get; set; }
}
