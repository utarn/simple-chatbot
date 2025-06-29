using Utharn.Library.Localizer;

namespace ChatbotApi.Application.Chatbots.Commands.CreateChatbotCommand;

public class CreateChatbotCommand : IRequest<bool>
{
    [Localize(Value = "ชื่อบอท")]
    public string Name { get; set; } = default!;

    [Localize(Value = "โทเคน Line")]
    public string? LineChannelAccessToken { get; set; }

    [Localize(Value = "Verify Token ของ Facebook")]
    public string? FacebookVerifyToken { get; set; }

    [Localize(Value = "Access Token ของ Facebook")]
    public string? FacebookAccessToken { get; set; }
    
    [Localize(Value = "ModelHarbor API key")]
    public string? LlmKey { get; set; }

    [Localize(Value = "System Role กำหนดให้ LLM")]
    public string? SystemRole { get; set; }

    public class CreateChatbotCommandHandler : IRequestHandler<CreateChatbotCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public CreateChatbotCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(CreateChatbotCommand request, CancellationToken cancellationToken)
        {
            var entity = new Chatbot
            {
                Name = request.Name,
                LineChannelAccessToken = request.LineChannelAccessToken,
                LlmKey = request.LlmKey,
                SystemRole = request.SystemRole,
                FacebookVerifyToken = request.FacebookVerifyToken,
                FacebookAccessToken = request.FacebookAccessToken,
                HistoryMinute = 15,
                AllowOutsideKnowledge = false,
                ShowReference = false,
                TopKDocument = 4,
                MaxChunkSize = 6000,
                MaxOverlappingSize = 200,
                MaximumDistance = 3,
                ResponsiveAgent = false,
                ModelName = "basic",
                EnableWebSearchTool = false,
            };

            await _context.Chatbots.AddAsync(entity, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
