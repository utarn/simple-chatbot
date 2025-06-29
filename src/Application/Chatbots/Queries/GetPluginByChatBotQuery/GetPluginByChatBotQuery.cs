using Utharn.Library.Localizer;
using ChatbotApi.Domain.Entities;

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

public class PluginViewModel
{
    public int ChatbotId { get; set; }
    [Localize(Value = "รหัสปลั๊กอิน")]
    public string PluginName { get; set; } = default!;
    [Localize(Value = "คำอธิบาย")]
    public string Description { get; set; } = default!;
    [Localize(Value = "สถานะ")]
    public bool IsEnabled { get; set; }

    public static PluginViewModel MappingFunction(ChatbotApi.Domain.Entities.ChatbotPlugin plugin)
    {
        return new PluginViewModel
        {
            ChatbotId = plugin.ChatbotId,
            PluginName = plugin.PluginName,
            // Description and IsEnabled are not directly mapped from ChatbotPlugin
            // They are derived from Systems.Plugins and the presence in the ChatbotPlugins collection
            // This mapping function is primarily for direct ChatbotPlugin to PluginViewModel conversion if needed elsewhere.
        };
    }
}
