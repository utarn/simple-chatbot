namespace ChatbotApi.Application.PlayLists.Queries.GetPlayListQuery;

public class GetPlayListQuery : IRequest<PaginatedList<PlayListViewModel, PlayListMetadata>>
{
    public int? Id { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchText { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public class GetPlayListQueryHandler : IRequestHandler<GetPlayListQuery, PaginatedList<PlayListViewModel, PlayListMetadata>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetPlayListQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedList<PlayListViewModel, PlayListMetadata>> Handle(GetPlayListQuery request, CancellationToken cancellationToken)
        {
            var query = _context.PlayLists.AsQueryable();

            // Apply filters
            if (request.Id.HasValue)
            {
                query = query.Where(x => x.Id == request.Id.Value);
            }

            if (request.StartDate.HasValue)
            {
                query = query.Where(x => x.CreatedDate >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(x => x.CreatedDate <= request.EndDate.Value);
            }

            if (!string.IsNullOrEmpty(request.SearchText))
            {
                query = query.Where(x => x.MusicName.Contains(request.SearchText) ||
                                        x.AlbumName.Contains(request.SearchText));
            }

            var metadata = new PlayListMetadata
            {
                Id = request.Id,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                SearchText = request.SearchText
            };

            var playLists = await query
                .OrderBy(x => x.Id)
                .ProjectTo<PlayListViewModel>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.Page, request.PageSize, metadata, cancellationToken);

            return playLists;
        }
    }
}