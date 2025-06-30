using Utharn.Library.Localizer;
using ChatbotApi.Domain.Entities;

namespace ChatbotApi.Application.Chatbots.Queries.GetErrorsQuery;

public class ErrorViewModel
{
    public int Id { get; set; }
    public int ChatBotId { get; set; }
    [Localize(Value = "ประเภท")]
    public string Type { get; set; } = null!;
    [Localize(Value = "วันที่เวลา")]
    public DateTime Created { get; set; }
    [Localize(Value = "ชื่อไฟล์")]
    public string? FileName { get; set; }
    [Localize(Value = "URL")]
    public string? Url { get; set; }
    [Localize(Value = "ใช้ CAG")]
    public bool? UseCag { get; set; }
    [Localize(Value = "ข้อความ Exception")]
    public string ExceptionMessage { get; set; } = null!;

    public static ErrorViewModel MappingFunction(ChatbotApi.Domain.Entities.ImportError error)
    {
        return new ErrorViewModel
        {
            Id = error.Id,
            ChatBotId = error.ChatBotId,
            Type = "ImportError",
            Created = error.Created.AddHours(7),
            FileName = error.FileName,
            ExceptionMessage = error.ExceptionMessage
        };
    }

    public static ErrorViewModel MappingFunction(ChatbotApi.Domain.Entities.RefreshInformation error)
    {
        return new ErrorViewModel
        {
            Id = error.Id,
            ChatBotId = error.ChatBotId,
            Type = "RefreshInformation",
            Created = error.Created.AddHours(7),
            Url = error.Url,
            UseCag = error.UseCag,
            ExceptionMessage = error.ExceptionMessage
        };
    }
}
