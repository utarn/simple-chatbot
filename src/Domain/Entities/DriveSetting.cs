namespace ChatbotApi.Domain.Entities;

public class DriveSetting
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public DateTime? IssuedUtc { get; set; }
    public long? ExpiresInSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}