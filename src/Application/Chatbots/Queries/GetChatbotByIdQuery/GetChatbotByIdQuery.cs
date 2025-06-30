
using Microsoft.Extensions.Logging;

namespace ChatbotApi.Application.Chatbots.Queries.GetChatbotByIdQuery;

public class GetChatbotByIdQuery : IRequest<ChatbotSingleViewModel>
{
    public int Id { get; set; }

    public bool ObtainLogo { get; set; }

    public class GetChatbotByIdQueryHandler : IRequestHandler<GetChatbotByIdQuery, ChatbotSingleViewModel>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IOpenAiService _openAiService;
        private readonly ILogger<GetChatbotByIdQueryHandler> _logger;

        public GetChatbotByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService,
            IOpenAiService openAiService, ILogger<GetChatbotByIdQueryHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _openAiService = openAiService;
            _logger = logger;
        }

        public async Task<ChatbotSingleViewModel> Handle(GetChatbotByIdQuery request,
            CancellationToken cancellationToken)
        {
            var entity = await _context.Chatbots
                .Include(c => c.ImportErrors)
                .Include(c => c.ChatbotPlugins)
                .FirstAsync(x => x.Id == request.Id, cancellationToken);

            return ChatbotSingleViewModel.MappingFunction(entity);
        }
    }
}
