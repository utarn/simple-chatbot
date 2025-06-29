using ChatbotApi.Domain.Entities;

namespace ChatbotApi.Infrastructure.OpenAI;

public class PreMessageResult
{
    public PreMessage PreMessage { get; set; } = default!;
    public int? GroupNumber { get; set; }
    public double Score { get; set; }
}
