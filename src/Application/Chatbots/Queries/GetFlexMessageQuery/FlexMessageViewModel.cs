using Utharn.Library.Localizer;

namespace ChatbotApi.Application.Chatbots.Queries.GetFlexMessageQuery;

public class FlexMessageViewModel
{
    public int Id { get; set; }
    [Localize(Value = "แชทบอท")]
    public int ChatbotId { get; set; }
    [Localize(Value = "ช่องทางบริการ")]
    public string Type { get; set; }
    [Localize(Value = "ข้อความหรือ Postback ที่ต้องการส่ง (ภาษาอังกฤษใช้ตัวอักษรเล็กเท่านั้น)")]
    public string Key { get; set; }
    [Localize(Value ="ลำดับข้อความ")]
    public int Order { get; set; }
    [Localize(Value = "ข้อความตอบกลับ")]
    public string JsonValue { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<FlexMessage, FlexMessageViewModel>();
        }
    }
}
