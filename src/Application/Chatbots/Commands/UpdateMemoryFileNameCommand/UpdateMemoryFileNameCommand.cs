namespace ChatbotApi.Application.Chatbots.Commands.UpdateMemoryFileNameCommand;

public class UpdateMemoryFileNameCommand : IRequest<bool>
{
    public int ChatbotId { get; set; }
    public string OriginalFileName { get; set; } = default!;
    public string NewFileName { get; set; } = default!;
}

public class UpdateMemoryFileNameCommandHandler 
    : IRequestHandler<UpdateMemoryFileNameCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateMemoryFileNameCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateMemoryFileNameCommand request, 
        CancellationToken cancellationToken)
    {
        var messages = await _context.PreMessages
            .Where(p => p.ChatBotId == request.ChatbotId && 
                        p.FileName == request.OriginalFileName)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.FileName = request.NewFileName;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
