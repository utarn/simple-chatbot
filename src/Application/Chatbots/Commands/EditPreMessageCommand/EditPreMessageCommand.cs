using OpenAiService.Interfaces;
using Utharn.Library.Localizer;

namespace ChatbotApi.Application.Chatbots.Commands.EditPreMessageCommand;

public class EditPreMessageCommand : IRequest<bool>
{
    // chatbot id
    public int Id { get; set; }

    [Localize(Value = "ลำดับ ห้ามใส่เลขซ้ำ")]
    public int Order { get; set; }

    [Localize(Value = "สิ่งที่บอทรู้ User Role")]
    public string UserMessage { get; set; } = default!;

    [Localize(Value = "ประโยคที่ต้องการให้บอทสอบถามผู้ใช้งาน Assistant Role")]
    public string AssistantMessage { get; set; } = default!;

    [Localize(Value = "องค์ความรู้จำเป็น")]
    public bool IsRequired { get; set; }

    [Localize(Value = "กำหนดการอัตโนมัติ (Cron Expression)")]
    public string? CronJob { get; set; }

    [Localize(Value = "ขนาดของข้อมูลที่แบ่ง (สูงสุด 8192)")]
    public int ChunkSize { get; set; }

    [Localize(Value = "ขนาดของข้อมูลที่กำหนดให้ซ้ำกัน")]
    public int OverlappingSize { get; set; }


    public class EditPreMessageCommandHandler : IRequestHandler<EditPreMessageCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly IOpenAiService _openAiService;

        public EditPreMessageCommandHandler(IApplicationDbContext context, IOpenAiService openAiService)
        {
            _context = context;
            _openAiService = openAiService;
        }

        public async Task<bool> Handle(EditPreMessageCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.PreMessages
                .Include(p => p.ChatBot)
                .Where(p => p.ChatBotId == request.Id && p.Order == request.Order)
                .FirstAsync(cancellationToken);

            bool isChanged = entity.UserMessage != request.UserMessage ||
                             entity.AssistantMessage != request.AssistantMessage;

            entity.UserMessage = request.UserMessage;
            entity.AssistantMessage = request.AssistantMessage;
            entity.IsRequired = request.IsRequired;
            entity.CronJob = request.CronJob;
            if (request.ChunkSize != 0)
            {
                entity.ChunkSize = request.ChunkSize;
            }

            if (request.OverlappingSize != 0)
            {
                entity.OverlappingSize = request.OverlappingSize;
            }

            if (entity.ChatBot.LlmKey != null && isChanged)
            {
                var embeddings = await _openAiService.CallEmbeddingsAsync(request.UserMessage, entity.ChatBot.LlmKey,
                    cancellationToken);
                if (embeddings != null)
                {
                    entity.Embedding = embeddings;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
