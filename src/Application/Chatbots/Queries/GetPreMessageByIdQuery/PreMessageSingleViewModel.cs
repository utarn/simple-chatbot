using Utharn.Library.Localizer;
using ChatbotApi.Domain.Entities;

namespace ChatbotApi.Application.Chatbots.Queries.GetPreMessageByIdQuery;

public class PreMessageSingleViewModel
{
    public string ChatBotName { get; set; } = default!;
    public int ChatBotId { get; set; }
    public int Order { get; set; }
    public string UserMessage { get; set; } = default!;
    public string AssistantMessage { get; set; } = default!;
    public bool IsRequired { get; set; }
    [Localize(Value = "ชื่อไฟล์")]
    public string? FileName { get; set; }
    public string? CronJob { get; set; }
    public string? Url { get; set; }
    public int ChunkSize { get; set; }
    public int OverlappingSize { get; set; }

    public static PreMessageSingleViewModel MappingFunction(ChatbotApi.Domain.Entities.PreMessage preMessage)
    {
        return new PreMessageSingleViewModel
        {
            ChatBotName = preMessage.ChatBot.Name,
            ChatBotId = preMessage.ChatBotId,
            Order = preMessage.Order,
            UserMessage = preMessage.UserMessage,
            AssistantMessage = preMessage.AssistantMessage ?? string.Empty,
            IsRequired = preMessage.IsRequired,
            FileName = preMessage.FileName,
            CronJob = preMessage.CronJob,
            Url = preMessage.Url,
            ChunkSize = preMessage.ChunkSize,
            OverlappingSize = preMessage.OverlappingSize
        };
    }
}
