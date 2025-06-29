namespace ChatbotApi.Application.Chatbots.Commands.DeleteChatbotCommand;

public class DeleteChatbotCommand : IRequest<bool>
{
    public int Id { get; set; }
    
    public class DeleteChatbotCommandHandler : IRequestHandler<DeleteChatbotCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeleteChatbotCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(DeleteChatbotCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.Chatbots
                .Include(x => x.MessageHistories)
                .FirstAsync(x => x.Id == request.Id, cancellationToken);

            _context.Chatbots.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
