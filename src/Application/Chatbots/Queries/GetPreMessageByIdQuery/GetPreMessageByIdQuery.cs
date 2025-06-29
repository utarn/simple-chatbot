using ChatbotApi.Domain.Entities;

namespace ChatbotApi.Application.Chatbots.Queries.GetPreMessageByIdQuery;

public class GetPreMessageByIdQuery : IRequest<PreMessageSingleViewModel>
{
    // chatbot id
    public int Id { get; set; }

    public int Order { get; set; }

    public class Handler : IRequestHandler<GetPreMessageByIdQuery, PreMessageSingleViewModel>
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PreMessageSingleViewModel> Handle(GetPreMessageByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _context.PreMessages
                .Include(p => p.ChatBot)
                .Where(p => p.ChatBotId == request.Id && p.Order == request.Order)
                .FirstAsync(cancellationToken);

            return PreMessageSingleViewModel.MappingFunction(entity);
        }
    }
}
