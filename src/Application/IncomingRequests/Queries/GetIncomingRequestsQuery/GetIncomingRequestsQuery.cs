namespace ChatbotApi.Application.IncomingRequests.Queries.GetIncomingRequestsQuery
{
    public class GetIncomingRequestsQuery : IRequest<PaginatedList<IncomingRequestListItemViewModel>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetIncomingRequestsQueryHandler : IRequestHandler<GetIncomingRequestsQuery, PaginatedList<IncomingRequestListItemViewModel>>
    {
        private readonly IApplicationDbContext _context;

        public GetIncomingRequestsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<IncomingRequestListItemViewModel>> Handle(
            GetIncomingRequestsQuery request, CancellationToken cancellationToken)
        {
            return await _context.IncomingRequests
                .OrderByDescending(r => r.Created)
                .Select(r => new IncomingRequestListItemViewModel
                {
                    Id = r.Id,
                    Created = r.Created,
                    Endpoint = r.Endpoint,
                    Channel = r.Channel,
                    Raw = r.Raw
                })
                .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
        }
    }
}
