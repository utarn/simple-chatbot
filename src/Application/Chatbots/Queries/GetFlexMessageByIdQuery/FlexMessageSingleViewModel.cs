using Utharn.Library.Localizer;
using ChatbotApi.Domain.Entities;

namespace ChatbotApi.Application.Chatbots.Queries.GetFlexMessageByIdQuery;

public class FlexMessageSingleViewModel
{
    public int Id { get; set; }

    [Localize(Value = "แชทบอท")]
    public string ChatbotName { get; set; }

    public int ChatbotId { get; set; }

    [Localize(Value = "ช่องทางบริการ")]
    public string Type { get; set; }

    [Localize(Value = "ข้อความหรือ Postback ที่ต้องการส่ง (ภาษาอังกฤษใช้ตัวอักษรเล็กเท่านั้น)")]
    public string Key { get; set; }

    [Localize(Value = "ลำดับข้อความ")]
    public int Order { get; set; }

    [Localize(Value = "ข้อความตอบกลับ")]
    public string JsonValue { get; set; }

    public static FlexMessageSingleViewModel MappingFunction(ChatbotApi.Domain.Entities.FlexMessage flexMessage)
    {
        return new FlexMessageSingleViewModel
        {
            Id = flexMessage.Id,
            ChatbotName = flexMessage.Chatbot.Name,
            ChatbotId = flexMessage.ChatbotId,
            Type = flexMessage.Type,
            Key = flexMessage.Key,
            Order = flexMessage.Order,
            JsonValue = flexMessage.JsonValue
        };
    }
}
