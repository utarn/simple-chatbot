using Utharn.Library.Localizer;

namespace ChatbotApi.Application.UserInternalProfiles.Commands.EditUserInternalProfileCommand;

public class EditUserInternalProfileCommand : IRequest<bool>
{
    public int Id { get; set; }
    
    [Localize(Value = "LINE User ID")]
    public string? LineUserId { get; set; }
    
    [Localize(Value = "คำนำหน้า")]
    public string? Initial { get; set; }
    
    [Localize(Value = "ชื่อ")]
    public string? FirstName { get; set; }
    
    [Localize(Value = "นามสกุล")]
    public string? LastName { get; set; }
    
    [Localize(Value = "กลุ่ม")]
    public string? Group { get; set; }
    
    [Localize(Value = "คณะ")]
    public string? Faculty { get; set; }
    
    [Localize(Value = "วิทยาเขต")]
    public string? Campus { get; set; }

    public class EditUserInternalProfileCommandHandler : IRequestHandler<EditUserInternalProfileCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public EditUserInternalProfileCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(EditUserInternalProfileCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.UserInternalProfiles
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException(nameof(UserInternalProfile), request.Id.ToString());
            }

            entity.LineUserId = request.LineUserId;
            entity.Initial = request.Initial;
            entity.FirstName = request.FirstName;
            entity.LastName = request.LastName;
            entity.Group = request.Group;
            entity.Faculty = request.Faculty;
            entity.Campus = request.Campus;

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}