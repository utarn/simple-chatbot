namespace ChatbotApi.Infrastructure.Processors.LineIdentityPreProcessor;

public class LineUserIdentity
{
    public string LineUserId { get; set; } = string.Empty;
    public string Initial { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Faculty { get; set; } = string.Empty;
    public string Campus { get; set; } = string.Empty;
}

public class CachedSheetData
{
    public List<LineUserIdentity> Users { get; set; } = new();
    public DateTime CachedAt { get; set; }
    public bool IsExpired(TimeSpan expiration) => DateTime.UtcNow - CachedAt > expiration;
}