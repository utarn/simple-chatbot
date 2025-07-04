using Utharn.Library.Localizer;

namespace ChatbotApi.Application.PlayLists.Commands.CreatePlayListCommand;

public class CreatePlayListCommand : IRequest<int>
{
    [Localize(Value = "ชื่อเพลง")]
    public string MusicName { get; set; } = default!;

    [Localize(Value = "ชื่ออัลบั้ม")]
    public string AlbumName { get; set; } = default!;

    public class CreatePlayListCommandHandler : IRequestHandler<CreatePlayListCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreatePlayListCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreatePlayListCommand request, CancellationToken cancellationToken)
        {
            var entity = new PlayList
            {
                MusicName = request.MusicName,
                AlbumName = request.AlbumName,
                CreatedDate = DateTime.Now
            };

            await _context.PlayLists.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
    }
}