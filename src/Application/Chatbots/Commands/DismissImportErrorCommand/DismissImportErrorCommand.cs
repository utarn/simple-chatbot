namespace ChatbotApi.Application.Chatbots.Commands.DismissImportErrorCommand;

public class DismissImportErrorCommand : IRequest<bool>
{
    public int Id { get; set; }
    
    public class DismissImportErrorCommandHandler : IRequestHandler<DismissImportErrorCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public DismissImportErrorCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<bool> Handle(DismissImportErrorCommand request, CancellationToken cancellationToken)
        {
            var error = await _context.ImportErrors
                .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

            if (error != null)
            {
                error.IsDismissed = true;
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false;
        }
    }
}
