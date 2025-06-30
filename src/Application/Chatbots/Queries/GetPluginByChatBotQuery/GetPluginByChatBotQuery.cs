using Utharn.Library.Localizer;
using ChatbotApi.Domain.Entities;
using ChatbotApi.Application.Common.Models;

namespace ChatbotApi.Application.Chatbots.Queries.GetPluginByChatBotQuery;

public class GetPluginByChatBotQuery  : IRequest<List<PluginViewModel>>
{
    public int Id { get; set; }

    
    public class GetPluginByChatBotQueryHandler : IRequestHandler<GetPluginByChatBotQuery, List<PluginViewModel>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetPluginByChatBotQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<PluginViewModel>> Handle(GetPluginByChatBotQuery request, CancellationToken cancellationToken)
        {
            var entity = await _context.ChatbotPlugins
                .Where(x => x.ChatbotId == request.Id)
                .ToListAsync(cancellationToken);

            var result = new List<PluginViewModel>();
            foreach (var (key, value) in Systems.Plugins)
            {
               if (entity.Any(x => x.PluginName == key))
               {
                   result.Add(new PluginViewModel()
                   {
                       ChatbotId = request.Id,
                       PluginName = key,
                       Description = value,
                       IsEnabled = true
                   });
               }
               else
               {
                   result.Add(new PluginViewModel()
                   {
                       ChatbotId = request.Id,
                       PluginName = key,
                       Description = value,
                       IsEnabled = false
                   });
               }
            }
            
            return result;
        }
    }
}
