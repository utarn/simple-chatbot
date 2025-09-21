namespace ChatbotApi.Application.UserInternalProfiles.Queries.GetUserInternalProfileByIdQuery;

public class GetUserInternalProfileByIdQuery : IRequest<UserInternalProfileSingleViewModel>
{
    public int Id { get; set; }

    public class GetUserInternalProfileByIdQueryHandler : IRequestHandler<GetUserInternalProfileByIdQuery, UserInternalProfileSingleViewModel>
    {
        private readonly IApplicationDbContext _context;

        public GetUserInternalProfileByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserInternalProfileSingleViewModel> Handle(GetUserInternalProfileByIdQuery request, CancellationToken cancellationToken)
        {
            var profile = await _context.UserInternalProfiles
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (profile == null)
            {
                throw new NotFoundException(nameof(UserInternalProfile), request.Id.ToString());
            }

            return UserInternalProfileSingleViewModel.MappingFunction(profile);
        }
    }
}