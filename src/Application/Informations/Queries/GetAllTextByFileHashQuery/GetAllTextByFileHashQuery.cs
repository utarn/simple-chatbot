using ChatbotApi.Application.Common.Models;

namespace ChatbotApi.Application.Informations.Queries.GetAllTextByFileHashQuery;

public class GetAllTextByFileHashQuery : IRequest<PageData>
{
    public string Id { get; set; } = null!;
}


public class GetAllTextByFileHashQueryHandler : IRequestHandler<GetAllTextByFileHashQuery,PageData>
{
    private readonly IApplicationDbContext _context;

    public GetAllTextByFileHashQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PageData> Handle(GetAllTextByFileHashQuery request, CancellationToken cancellationToken)
    {
        var fileContents = await _context.PreMessages
            .Include(x => x.PreMessageContent)
            .Where(x => x.FileHash == request.Id)
            .OrderBy(x => x.Order)
            .ToListAsync(cancellationToken);
        
        // join with new line
        var result = new PageData() { Text = string.Join("\n", fileContents.Select(u => u.UserMessage)) };
        if (fileContents.Count == 1 && fileContents[0].FileMimeType!.StartsWith("image/"))
        {
            result.ImageBase64 = Convert.ToBase64String(fileContents[0].PreMessageContent?.Content ?? []);
        }
        return result;
    }
}
