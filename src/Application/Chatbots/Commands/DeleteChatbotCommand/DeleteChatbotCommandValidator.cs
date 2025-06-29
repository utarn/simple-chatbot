namespace ChatbotApi.Application.Chatbots.Commands.DeleteChatbotCommand;

public class DeleteChatbotCommandValidator : AbstractValidator<DeleteChatbotCommand>
{
    public DeleteChatbotCommandValidator(IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        RuleFor(v => v.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("รหัสบอทต้องไม่ว่าง");
    }
}
