namespace ChatbotApi.Application.Chatbots.Queries.GetMemoryFileQuery;

public class GetMemoryFileQuery : IRequest<List<MemoryFileViewModel>>
{
    public int Id { get; set; } // Chatbot ID
}

public class GetMemoryFileQueryHandler : IRequestHandler<GetMemoryFileQuery, List<MemoryFileViewModel>>
{
    private readonly IApplicationDbContext _context;

    public GetMemoryFileQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MemoryFileViewModel>> Handle(GetMemoryFileQuery request, CancellationToken cancellationToken)
    {
        return await _context.PreMessages
            .Where(p => p.ChatBotId == request.Id && p.FileName != null && p.FileHash != null)
            .GroupBy(p => new { p.FileHash, p.FileName })
            .Select(g => new MemoryFileViewModel
            {
                FileName = g.Key.FileName!,
                FileHash = g.Key.FileHash!,
                EntryCount = g.Count()
            })
            .ToListAsync(cancellationToken);
    }
}
