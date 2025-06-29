using Microsoft.AspNetCore.Http;
using Utharn.Library.Localizer;

namespace ChatbotApi.Application.Chatbots.Commands.EditChatbotCommand;

public class EditChatbotCommand : IRequest<bool>
{
    public int Id { get; set; }

    [Localize(Value = "ชื่อบอท")]
    public string Name { get; set; } = default!;

    [Localize(Value = "โทเคน Line")]
    public string? LineChannelAccessToken { get; set; }
    [Localize(Value = "ModelHarbor API key")]
    public string? LlmKey { get; set; }

    [Localize(Value = "System Role กำหนดให้ LLM")]
    public string? SystemRole { get; set; }

    [Localize(Value = "Verify Token ของ Facebook")]
    public string? FacebookVerifyToken { get; set; }

    [Localize(Value = "Access Token ของ Facebook")]
    public string? FacebookAccessToken { get; set; }
    
    [Localize(Value = "ขนาดของข้อมูลที่แบ่ง (สูงสุด 8192, ค่าเริ่มต้น: 2000)")]    
    public int? MaxChunkSize { get; set; }
    
    [Localize(Value = "ขนาดของข้อมูลที่กำหนดให้ซ้ำกัน (สูงสุด: 500, ค่าเริ่มต้น: 200)")]
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
    public bool AllowOutsideKnowledge { get; set; }
    [Localize(Value = "ตอบคำถามเมื่อถาม และถามต่อเมื่อตอบ (Responsive Agent)")]    
    public bool ResponsiveAgent { get; set; } 
    [Localize(Value = "ชนิดของโมเดล")]
    public string? ModelName { get; set; }
    [Localize(Value = "ใช้ Web Search Tool")]    
    public bool EnableWebSearchTool { get; set; }


    public class EditChatbotCommandHandler : IRequestHandler<EditChatbotCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public EditChatbotCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(EditChatbotCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.Chatbots
                .FirstAsync(c => c.Id == request.Id, cancellationToken);

            entity.Name = request.Name;
            entity.LineChannelAccessToken = request.LineChannelAccessToken;
            entity.LlmKey = request.LlmKey;
            entity.SystemRole = request.SystemRole;
            entity.FacebookVerifyToken = request.FacebookVerifyToken;
            entity.FacebookAccessToken = request.FacebookAccessToken;
            entity.MaxChunkSize = request.MaxChunkSize;
            entity.MaxOverlappingSize = request.MaxOverlappingSize;
            entity.TopKDocument = request.TopKDocument;
            entity.MaximumDistance = request.MaximumDistance;
            entity.ShowReference = request.ShowReference;
            entity.ProtectedApiKey = request.ProtectedApiKey;
            entity.HistoryMinute = request.HistoryMinute;
            entity.AllowOutsideKnowledge = request.AllowOutsideKnowledge;
            entity.ResponsiveAgent = request.ResponsiveAgent;
            entity.ModelName = request.ModelName;
            entity.EnableWebSearchTool = request.EnableWebSearchTool;
            
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
