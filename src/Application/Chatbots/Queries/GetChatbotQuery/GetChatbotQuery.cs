namespace ChatbotApi.Application.Chatbots.Queries.GetChatbotQuery;

public class GetChatbotQuery : IRequest<PaginatedList<ChatbotViewModel, ChatbotMetadata>>
{
    public string? Name { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = Systems.PageSize;

    public class
        GetChatbotQueryHandler : IRequestHandler<GetChatbotQuery, PaginatedList<ChatbotViewModel, ChatbotMetadata>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public GetChatbotQueryHandler(IApplicationDbContext context, IMapper mapper,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<PaginatedList<ChatbotViewModel, ChatbotMetadata>> Handle(GetChatbotQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Chatbots
                .Include(x => x.ChatbotPlugins)
                .Include(x => x.FlexMessages)
                .Include(x => x.MessageHistories)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Name))
            {
                query = query.Where(x => x.Name.Contains(request.Name));
            }

            var metadata = new ChatbotMetadata { Name = request.Name };

            var chatbots = await query
                .OrderBy(x => x.Name)
                .ProjectTo<ChatbotViewModel>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.Page, request.PageSize, metadata, cancellationToken);

            return chatbots;
        }
    }
}
