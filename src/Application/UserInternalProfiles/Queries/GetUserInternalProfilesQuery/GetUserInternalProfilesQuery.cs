namespace ChatbotApi.Application.UserInternalProfiles.Queries.GetUserInternalProfilesQuery;

public class GetUserInternalProfilesQuery : IRequest<PaginatedList<UserInternalProfileViewModel, UserInternalProfileMetadata>>
{
    public string? LineUserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = Systems.PageSize;

    public class GetUserInternalProfilesQueryHandler : IRequestHandler<GetUserInternalProfilesQuery, PaginatedList<UserInternalProfileViewModel, UserInternalProfileMetadata>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetUserInternalProfilesQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedList<UserInternalProfileViewModel, UserInternalProfileMetadata>> Handle(GetUserInternalProfilesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.UserInternalProfiles.AsQueryable();

            if (!string.IsNullOrEmpty(request.LineUserId))
            {
                query = query.Where(x => x.LineUserId.Contains(request.LineUserId));
            }

            if (!string.IsNullOrEmpty(request.FirstName))
            {
                query = query.Where(x => x.FirstName.Contains(request.FirstName));
            }

            if (!string.IsNullOrEmpty(request.LastName))
            {
                query = query.Where(x => x.LastName.Contains(request.LastName));
            }

            var metadata = new UserInternalProfileMetadata 
            { 
                LineUserId = request.LineUserId,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            var profiles = await query
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ProjectTo<UserInternalProfileViewModel>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.Page, request.PageSize, metadata, cancellationToken);

            return profiles;
        }
    }
}