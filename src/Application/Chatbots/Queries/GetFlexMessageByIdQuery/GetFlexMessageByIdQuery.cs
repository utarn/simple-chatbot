using ChatbotApi.Domain.Entities;

namespace ChatbotApi.Application.Chatbots.Queries.GetFlexMessageByIdQuery;

public class GetFlexMessageByIdQuery : IRequest<FlexMessageSingleViewModel>
{
    public int Id { get; set; }

    public class GetFlexMessageByIdQueryHandler : IRequestHandler<GetFlexMessageByIdQuery, FlexMessageSingleViewModel>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetFlexMessageByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<FlexMessageSingleViewModel> Handle(GetFlexMessageByIdQuery request,
            CancellationToken cancellationToken)
        {
            var entity = await _context.FlexMessages
                .Include(x => x.Chatbot)
                .FirstAsync(x => x.Id == request.Id, cancellationToken);

            return FlexMessageSingleViewModel.MappingFunction(entity);
        }
    }
}
