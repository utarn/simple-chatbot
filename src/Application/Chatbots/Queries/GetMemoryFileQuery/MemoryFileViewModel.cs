namespace ChatbotApi.Application.Chatbots.Queries.GetMemoryFileQuery;

public class MemoryFileViewModel
{
    public string FileName { get; set; } = default!;
    public string FileHash { get; set; } = default!;
    public int EntryCount { get; set; }
}
