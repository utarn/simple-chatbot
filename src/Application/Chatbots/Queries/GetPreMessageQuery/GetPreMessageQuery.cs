namespace ChatbotApi.Application.Chatbots.Queries.GetPreMessageQuery;

public class GetPreMessageQuery : IRequest<PaginatedList<PreMessageViewModel, PreMessageMetadata>>
{
    public int Id { get; set; }
    public string? UserMessage { get; set; }
    public string? AssistantMessage { get; set; } = default!;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = Systems.PageSize;

    public class GetPreMessageQueryHandler : IRequestHandler<GetPreMessageQuery,
        PaginatedList<PreMessageViewModel, PreMessageMetadata>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetPreMessageQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedList<PreMessageViewModel, PreMessageMetadata>> Handle(GetPreMessageQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.PreMessages
                .Where(p => p.ChatBotId == request.Id)
                .OrderBy(p => !p.IsRequired)
                .ThenBy(p => p.Order)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.UserMessage))
            {
                query = query.Where(x => x.UserMessage.Contains(request.UserMessage));
            }

            if (!string.IsNullOrEmpty(request.AssistantMessage))
            {
                query = query.Where(x => x.AssistantMessage != null && x.AssistantMessage.Contains(request.AssistantMessage));
            }

            var metadata = new PreMessageMetadata
            {
                UserMessage = request.UserMessage,
                AssistantMessage = request.AssistantMessage
            };

            var preMessages = await query
                .ProjectTo<PreMessageViewModel>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.Page, request.PageSize, metadata, cancellationToken);

            return preMessages;
        }
    }
}
