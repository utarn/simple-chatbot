using Utharn.Library.Localizer;

namespace ChatbotApi.Application.UserInternalProfiles.Commands.CreateUserInternalProfileCommand;

public class CreateUserInternalProfileCommand : IRequest<bool>
{
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

    public class CreateUserInternalProfileCommandHandler : IRequestHandler<CreateUserInternalProfileCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public CreateUserInternalProfileCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(CreateUserInternalProfileCommand request, CancellationToken cancellationToken)
        {
            var entity = new UserInternalProfile
            {
                LineUserId = request.LineUserId,
                Initial = request.Initial,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Group = request.Group,
                Faculty = request.Faculty,
                Campus = request.Campus
            };

            await _context.UserInternalProfiles.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}