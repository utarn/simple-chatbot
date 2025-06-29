using Utharn.Library.Localizer;

namespace ChatbotApi.Application.Chatbots.Queries.GetPreMessageQuery;

public class PreMessageViewModel
{
    [Localize(Value = "รหัสแชทบอท")] public int ChatBotId { get; set; }

    [Localize(Value = "ลำดับ ห้ามใส่เลขซ้ำ")]
    public int Order { get; set; }

    [Localize(Value = "ความรู้ของบอทของ AI")]
    public string UserMessage { get; set; } = default!;

    [Localize(Value = "คำตอบของ AI")] 
    public string AssistantMessage { get; set; } = default!;

    [Localize(Value = "องค์ความรู้จำเป็น")]
    public bool IsRequired { get; set; }

    [Localize(Value = "ชื่อไฟล์")] 
    public string? FileName { get; set; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<PreMessage, PreMessageViewModel>();
        }
    }
}
