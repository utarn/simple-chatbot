// DeleteMemoryFileCommand.cs

namespace ChatbotApi.Application.Chatbots.Commands.DeleteMemoryFileCommand;

public class DeleteMemoryFileCommand : IRequest<bool>
{
    public int ChatbotId { get; set; }
    public string FileHash { get; set; } = default!;
}

public class DeleteMemoryFileCommandHandler 
    : IRequestHandler<DeleteMemoryFileCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteMemoryFileCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteMemoryFileCommand request, 
        CancellationToken cancellationToken)
    {
        var messages = await _context.PreMessages
            .Where(p => p.ChatBotId == request.ChatbotId && 
                        p.FileHash == request.FileHash)
            .ToListAsync(cancellationToken);

        _context.PreMessages.RemoveRange(messages);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
