namespace ChatbotApi.Application.Chatbots.Commands.TogglePluginStateCommand;

public class TogglePluginStateCommand : IRequest<bool>
{
    public int ChatbotId { get; set; }
    public string PluginName { get; set; } = default!;

    public class Handler : IRequestHandler<TogglePluginStateCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(TogglePluginStateCommand request, CancellationToken cancellationToken)
        {
            var plugin = await _context.ChatbotPlugins
                .FirstOrDefaultAsync(x => x.ChatbotId == request.ChatbotId && x.PluginName == request.PluginName,
                    cancellationToken);

            if (plugin != null)
            {
                _context.ChatbotPlugins.Remove(plugin);
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }

            var toAdd = new ChatbotPlugin { ChatbotId = request.ChatbotId, PluginName = request.PluginName, };
            await _context.ChatbotPlugins.AddAsync(toAdd, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
