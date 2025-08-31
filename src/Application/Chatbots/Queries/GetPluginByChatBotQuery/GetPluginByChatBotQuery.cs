using Utharn.Library.Localizer;
using ChatbotApi.Domain.Entities;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Application.Common.Services;

namespace ChatbotApi.Application.Chatbots.Queries.GetPluginByChatBotQuery;

public class GetPluginByChatBotQuery  : IRequest<List<PluginViewModel>>
{
    public int Id { get; set; }

    
    public class GetPluginByChatBotQueryHandler : IRequestHandler<GetPluginByChatBotQuery, List<PluginViewModel>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPluginDiscoveryService _pluginDiscoveryService;

        public GetPluginByChatBotQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IPluginDiscoveryService pluginDiscoveryService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _pluginDiscoveryService = pluginDiscoveryService;
        }

        public async Task<List<PluginViewModel>> Handle(GetPluginByChatBotQuery request, CancellationToken cancellationToken)
        {
            var entity = await _context.ChatbotPlugins
                .Where(x => x.ChatbotId == request.Id)
                .ToListAsync(cancellationToken);

            var result = new List<PluginViewModel>();

            // Get all discovered plugins
            
            var availablePlugins = _pluginDiscoveryService.GetAvailablePlugins();
            
            foreach (var (pluginName, pluginInfo) in availablePlugins)
            {
               if (entity.Any(x => x.PluginName == pluginName))
               {
                   result.Add(new PluginViewModel()
                   {
                       ChatbotId = request.Id,
                       PluginName = pluginName,
                       Description = pluginInfo.Description,
                       IsEnabled = true
                   });
               }
               else
               {
                   result.Add(new PluginViewModel()
                   {
                       ChatbotId = request.Id,
                       PluginName = pluginName,
                       Description = pluginInfo.Description,
                       IsEnabled = false
                   });
               }
            }
            
            return result;
        }
    }
}
