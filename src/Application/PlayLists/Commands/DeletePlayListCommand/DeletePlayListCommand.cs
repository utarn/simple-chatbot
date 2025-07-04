namespace ChatbotApi.Application.PlayLists.Commands.DeletePlayListCommand;

public class DeletePlayListCommand : IRequest<bool>
{
    public int Id { get; set; }

    public class DeletePlayListCommandHandler : IRequestHandler<DeletePlayListCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public DeletePlayListCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeletePlayListCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.PlayLists
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException(nameof(PlayList), request.Id.ToString());
            }

            _context.PlayLists.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}