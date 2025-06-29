namespace ChatbotApi.Application.Chatbots.Commands.DeleteFlexMessageCommand;

public class DeleteFlexMessageCommand : IRequest<bool>
{
    public int Id { get; set; }

    public class DeleteFlexMessageCommandHandler : IRequestHandler<DeleteFlexMessageCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public DeleteFlexMessageCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteFlexMessageCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.FlexMessages
                .Where(x => x.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                return false;
            }

            _context.FlexMessages.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
    
}
