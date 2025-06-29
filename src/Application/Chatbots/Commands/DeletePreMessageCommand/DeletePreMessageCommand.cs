namespace ChatbotApi.Application.Chatbots.Commands.DeletePreMessageCommand;

public class DeletePreMessageCommand : IRequest<bool>
{
    // chatbotid + : + order
    public string Id { get; set; }

    public class DeletePreMessageCommandHandler : IRequestHandler<DeletePreMessageCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeletePreMessageCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(DeletePreMessageCommand request, CancellationToken cancellationToken)
        {
            var id = request.Id.Split(":");
            var entity = await _context.PreMessages
                .Include(x => x.ChatBot)
                .Where(x => x.ChatBotId == int.Parse(id[0]) && x.Order == int.Parse(id[1]))
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException(nameof(PreMessage), request.Id);
            }

            _context.PreMessages.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
