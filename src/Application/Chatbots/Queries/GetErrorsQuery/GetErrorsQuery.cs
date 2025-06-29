using Microsoft.Extensions.Logging;
using ChatbotApi.Domain.Entities;

namespace ChatbotApi.Application.Chatbots.Queries.GetErrorsQuery;

public class GetErrorsQuery : IRequest<List<ErrorViewModel>>
{
    public int Id { get; set; }

    public class GetErrorsQueryHandler : IRequestHandler<GetErrorsQuery, List<ErrorViewModel>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetErrorsQueryHandler> _logger;

        public GetErrorsQueryHandler(IApplicationDbContext context, ILogger<GetErrorsQueryHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ErrorViewModel>> Handle(GetErrorsQuery request, CancellationToken cancellationToken)
        {
            // Get ImportErrors
            var importErrors = await _context.ImportErrors
                .Where(c => c.ChatBotId == request.Id && !c.IsDismissed)
                .ToListAsync(cancellationToken);

            // Get RefreshInformation errors
            var refreshErrors = await _context.RefreshInformation
                .Where(c => c.ChatBotId == request.Id && !c.IsDismissed)
                .ToListAsync(cancellationToken);

            // Combine the results
            var allErrors = importErrors.Select(ErrorViewModel.MappingFunction)
                .Concat(refreshErrors.Select(ErrorViewModel.MappingFunction))
                .OrderByDescending(e => e.Created).ToList();

            return allErrors;
        }
    }
}
