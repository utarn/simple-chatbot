using Utharn.Library.Localizer;

namespace ChatbotApi.Application.PlayLists.Commands.EditPlayListCommand;

public class EditPlayListCommand : IRequest<bool>
{
    public int Id { get; set; }

    [Localize(Value = "ชื่อเพลง")]
    public string MusicName { get; set; } = default!;

    [Localize(Value = "ชื่ออัลบั้ม")]
    public string AlbumName { get; set; } = default!;

    public class EditPlayListCommandHandler : IRequestHandler<EditPlayListCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public EditPlayListCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(EditPlayListCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.PlayLists
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException(nameof(PlayList), request.Id.ToString());
            }

            entity.MusicName = request.MusicName;
            entity.AlbumName = request.AlbumName;

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}