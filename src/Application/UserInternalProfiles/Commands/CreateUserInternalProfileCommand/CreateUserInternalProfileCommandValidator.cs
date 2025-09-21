namespace ChatbotApi.Application.UserInternalProfiles.Commands.CreateUserInternalProfileCommand;

public class CreateUserInternalProfileCommandValidator : AbstractValidator<CreateUserInternalProfileCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateUserInternalProfileCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.LineUserId)
            .MaximumLength(50).WithMessage("LINE User ID must not exceed 50 characters.")
            .MustAsync(BeUniqueLineUserId).WithMessage("The specified LINE User ID already exists.")
            .When(x => !string.IsNullOrEmpty(x.LineUserId));

        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Initial)
            .MaximumLength(10).WithMessage("Initial must not exceed 10 characters.");

        RuleFor(x => x.Group)
            .MaximumLength(100).WithMessage("Group must not exceed 100 characters.");

        RuleFor(x => x.Faculty)
            .MaximumLength(100).WithMessage("Faculty must not exceed 100 characters.");

        RuleFor(x => x.Campus)
            .MaximumLength(100).WithMessage("Campus must not exceed 100 characters.");
    }

    public async Task<bool> BeUniqueLineUserId(string? lineUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(lineUserId))
        {
            return true;
        }
        
        return await _context.UserInternalProfiles
            .AllAsync(x => x.LineUserId != lineUserId, cancellationToken);
    }
}