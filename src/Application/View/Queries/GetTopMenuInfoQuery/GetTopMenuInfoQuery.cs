using Microsoft.Extensions.Caching.Distributed;
using ChatbotApi.Domain.Entities;

namespace ChatbotApi.Application.View.Queries.GetTopMenuInfoQuery;

public record GetTopMenuInfoQuery() : IRequest<TopMenuInfoViewModel>;

public class GetTopMenuInfoQueryHandler : IRequestHandler<GetTopMenuInfoQuery, TopMenuInfoViewModel>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDistributedCache _cache;

    public GetTopMenuInfoQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IDistributedCache cache)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cache = cache;
    }

    public async Task<TopMenuInfoViewModel> Handle(GetTopMenuInfoQuery request, CancellationToken cancellationToken)
    {
        TopMenuInfoViewModel? cached =
            await _cache.GetObjectAsync<TopMenuInfoViewModel>($"topmenu:{_currentUserService.UserId}");
        if (cached != null)
        {
            return cached;
        }

        var user = await _context.Users
            .Where(u => u.Id == _currentUserService.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (user == null)
        {
            return new TopMenuInfoViewModel()
            {
                Email = "Anonymous", FullName = "Anonymous"
            };
        }

        var result = TopMenuInfoViewModel.MappingFunction(user);
        await _cache.SetObjectAsync($"topmenu:{_currentUserService.UserId}", result);
        return result;
    }
}
