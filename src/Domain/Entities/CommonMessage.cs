using Utharn.Library.Localizer;

namespace ChatbotApi.Domain.Entities;

public class FlexMessage
{
    public int Id { get; set; }
    [Localize(Value = "แชทบอท")]
    public Chatbot Chatbot { get; set; }
    public int ChatbotId { get; set; }
    [Localize(Value = "ช่องทางบริการ")]
    public string Type { get; set; }
    [Localize(Value = "ข้อความหรือ Postback ที่ต้องการส่ง (ภาษาอังกฤษใช้ตัวอักษรเล็กเท่านั้น)")]
    public string Key { get; set; }
    [Localize(Value = "ลำดับข้อความ")]
    public int Order { get; set; }
    [Localize(Value = "ข้อความตอบกลับ")]
    public string JsonValue { get; set; }

}


// Line Flex Message: https://developers.line.biz/flex-simulator/

