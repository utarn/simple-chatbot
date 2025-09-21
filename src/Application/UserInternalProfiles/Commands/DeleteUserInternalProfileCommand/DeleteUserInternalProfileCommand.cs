namespace ChatbotApi.Application.UserInternalProfiles.Commands.DeleteUserInternalProfileCommand;

public class DeleteUserInternalProfileCommand : IRequest<bool>
{
    public int Id { get; set; }

    public class DeleteUserInternalProfileCommandHandler : IRequestHandler<DeleteUserInternalProfileCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public DeleteUserInternalProfileCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteUserInternalProfileCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.UserInternalProfiles
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException(nameof(UserInternalProfile), request.Id.ToString());
            }

            _context.UserInternalProfiles.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}