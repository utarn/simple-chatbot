namespace ChatbotApi.Application.Chatbots.Commands.DeletePreMessageCommand;

public class DeletePreMessageCommandValidator : AbstractValidator<DeletePreMessageCommand>
{
    public DeletePreMessageCommandValidator(IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        RuleFor(v => v.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("รหัสข้อความต้องไม่ว่าง");

    }
}
