using Utharn.Library.Localizer;
using ChatbotApi.Domain.Entities;
using System.Linq.Expressions;

namespace ChatbotApi.Application.Chatbots.Queries.GetChatbotByIdQuery;

public class ChatbotSingleViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? LineChannelAccessToken { get; set; }
    public string? GoogleChatServiceAccount { get; set; }
    public string? FacebookVerifyToken { get; set; }
    public string? FacebookAccessToken { get; set; }
    public string? LlmKey { get; set; }
    public string? SystemRole { get; set; }

    public string? Logo { get; set; }
    [Localize(Value = "ขนาดของข้อมูลที่แบ่ง (สูงสุด 8192)")]
    public int? MaxChunkSize { get; set; }

    [Localize(Value = "ขนาดของข้อมูลที่กำหนดให้ซ้ำกัน")]
    public int? MaxOverlappingSize { get; set; }

    [Localize(Value = "จำนวนข้อมูลที่ดึงมา (ตั้งต้น: 4)")]
    public int? TopKDocument { get; set; }

    [Localize(Value = "ระยะห่างขั้นสูงของ Embeddings")]
    public double? MaximumDistance { get; set; }
    [Localize(Value = "แสดงข้อมูลอ้างอิง")]
    public bool? ShowReference { get; set; }
    [Localize(Value = "API Key ที่ป้องกัน OpenAI Endpoint")]
    public string? ProtectedApiKey { get; set; }
    [Localize(Value = "ประวัติย้อนหลังแชตที่รวมเข้าเป็นแชตเดียวย้อนหลัง (นาที)")]
    public int? HistoryMinute { get; set; }
    [Localize(Value = "อนุญาตองค์ความรู้ภายนอก")]
    public bool? AllowOutsideKnowledge { get; set; }
    [Localize(Value = "ตอบคำถามเมื่อถาม และถามต่อเมื่อตอบ (Responsive Agent)")]
    public bool ResponsiveAgent { get; set; }
    [Localize(Value = "ชนิดของโมเดล")]
    public string? ModelName { get; set; }
    [Localize(Value = "ใช้ Web Search Tool")]
    public bool EnableWebSearchTool { get; set; }

    public bool HasImportError { get; set; }
    public List<string> Plugins { get; set; } = new List<string>();

    public static ChatbotSingleViewModel MappingFunction(Chatbot chatbot)
    {
        return new ChatbotSingleViewModel
        {
            Id = chatbot.Id,
            Name = chatbot.Name,
            LineChannelAccessToken = chatbot.LineChannelAccessToken,
            GoogleChatServiceAccount = chatbot.GoogleChatServiceAccount,
            FacebookVerifyToken = chatbot.FacebookVerifyToken,
            FacebookAccessToken = chatbot.FacebookAccessToken,
            LlmKey = chatbot.LlmKey,
            SystemRole = chatbot.SystemRole,
            Logo = chatbot.Logo,
            MaxChunkSize = chatbot.MaxChunkSize,
            MaxOverlappingSize = chatbot.MaxOverlappingSize,
            TopKDocument = chatbot.TopKDocument,
            MaximumDistance = chatbot.MaximumDistance,
            ShowReference = chatbot.ShowReference,
            ProtectedApiKey = chatbot.ProtectedApiKey,
            HistoryMinute = chatbot.HistoryMinute,
            AllowOutsideKnowledge = chatbot.AllowOutsideKnowledge,
            ResponsiveAgent = chatbot.ResponsiveAgent,
            ModelName = chatbot.ModelName,
            EnableWebSearchTool = chatbot.EnableWebSearchTool,
            HasImportError = chatbot.ImportErrors.Any(i => !i.IsDismissed),
            Plugins = chatbot.ChatbotPlugins.Select(x => x.PluginName).ToList()
        };
    }
}
