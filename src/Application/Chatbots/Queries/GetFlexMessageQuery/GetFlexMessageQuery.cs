namespace ChatbotApi.Application.Chatbots.Queries.GetFlexMessageQuery;

public class GetFlexMessageQuery : IRequest<PaginatedList<FlexMessageViewModel, FlexMessageMetadata>>
{
    public int ChatbotId { get; set; }
    public string? Type { get; set; }
    public string? Key { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = Systems.PageSize;

    public class GetFlexMessageQueryHandler : IRequestHandler<GetFlexMessageQuery, PaginatedList<FlexMessageViewModel, FlexMessageMetadata>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public GetFlexMessageQueryHandler(IApplicationDbContext context, IMapper mapper,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<PaginatedList<FlexMessageViewModel, FlexMessageMetadata>> Handle(GetFlexMessageQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.FlexMessages
                .Include(x => x.Chatbot)
                .Where(x => x.ChatbotId == request.ChatbotId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Type))
            {
                query = query.Where(x => x.Type.Contains(request.Type));
            }

            if (!string.IsNullOrEmpty(request.Key))
            {
                query = query.Where(x => x.Key.Contains(request.Key));
            }

            var metadata = new FlexMessageMetadata { Type = request.Type, Key = request.Key };

            var flexMessages = await query
                .OrderBy(x => x.Order)
                .ProjectTo<FlexMessageViewModel>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.Page, request.PageSize, metadata, cancellationToken);

            return flexMessages;
        }
    }
}
