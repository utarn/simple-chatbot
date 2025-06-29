using Utharn.Library.Localizer;

namespace ChatbotApi.Application.Chatbots.Queries.GetFlexMessageQuery;

public class FlexMessageMetadata
{
    [Localize(Value = "ช่องทางบริการ")]
    public string? Type { get; set; }
    [Localize(Value = "ข้อความหรือ Postback ที่ต้องการส่ง (ภาษาอังกฤษใช้ตัวอักษรเล็กเท่านั้น)")]
    public string? Key { get; set; }
}
