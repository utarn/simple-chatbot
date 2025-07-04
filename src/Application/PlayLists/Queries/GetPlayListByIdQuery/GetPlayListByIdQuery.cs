namespace ChatbotApi.Application.PlayLists.Queries.GetPlayListByIdQuery;

public class GetPlayListByIdQuery : IRequest<PlayListDetailViewModel>
{
    public int Id { get; set; }

    public class GetPlayListByIdQueryHandler : IRequestHandler<GetPlayListByIdQuery, PlayListDetailViewModel>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetPlayListByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PlayListDetailViewModel> Handle(GetPlayListByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _context.PlayLists
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException(nameof(PlayList), request.Id.ToString());
            }

            return _mapper.Map<PlayListDetailViewModel>(entity);
        }
    }
}