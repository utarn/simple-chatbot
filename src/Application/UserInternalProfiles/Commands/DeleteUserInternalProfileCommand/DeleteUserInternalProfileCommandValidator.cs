namespace ChatbotApi.Application.UserInternalProfiles.Commands.DeleteUserInternalProfileCommand;

public class DeleteUserInternalProfileCommandValidator : AbstractValidator<DeleteUserInternalProfileCommand>
{
    public DeleteUserInternalProfileCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID is required.")
            .GreaterThan(0).WithMessage("ID must be greater than 0.");
    }
}