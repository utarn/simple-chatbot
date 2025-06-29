using OpenAiService.Interfaces;
using Utharn.Library.Localizer;

namespace ChatbotApi.Application.Chatbots.Commands.CreatePreMessageCommand;

public class CreatePreMessageCommand : IRequest<bool>
{
    // ChatbotId
    public int Id { get; set; }
    [Localize(Value = "ลำดับ ห้ามใส่เลขซ้ำ")]
    public int Order { get; set; }
    [Localize(Value = "ความรู้ของบอทของ AI")]
    public string UserMessage { get; set; } = default!;
    [Localize(Value = "องค์ความรู้จำเป็น")]
    public bool IsRequired { get; set; }
    public class AddPreMessageCommandHandler : IRequestHandler<CreatePreMessageCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IOpenAiService _openAiService;

        public AddPreMessageCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IOpenAiService openAiService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _openAiService = openAiService;
        }

        public async Task<bool> Handle(CreatePreMessageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var chatbot = await _context.Chatbots
                    .FirstAsync(c => c.Id == request.Id, cancellationToken);

                var entity = new PreMessage
                {
                    ChatBotId = request.Id,
                    Order = request.Order,
                    UserMessage = request.UserMessage,
                    AssistantMessage = string.Empty,
                    IsRequired = request.IsRequired,
                    ChunkSize = chatbot.MaxChunkSize ?? 8000,
                    OverlappingSize = chatbot.MaxOverlappingSize ?? 200,
                };

                if (chatbot.LlmKey != null)
                {
                    var embeddings = await _openAiService.CallEmbeddingsAsync(request.UserMessage, chatbot.LlmKey, cancellationToken);
                    if (embeddings != null)
                    {
                        entity.Embedding = embeddings;
                    }
                }
                await _context.PreMessages.AddAsync(entity, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }
}
