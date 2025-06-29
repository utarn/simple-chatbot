namespace ChatbotApi.Application.Chatbots.Commands.EditChatbotCommand;

public class EditChatbotCommandValidator : AbstractValidator<EditChatbotCommand>
{
    public EditChatbotCommandValidator(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        RuleFor(v => v.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("ชื่อบอทต้องไม่ว่าง")
            ;

        RuleFor(v => v.Id)
            .Cascade(CascadeMode.Stop)
            .MustAsync(async (id, cancellationToken) =>
            {
                return await context.Chatbots
                    .AnyAsync(x => x.Id == id, cancellationToken);
            }).WithMessage("ไม่พบแชทบอทที่ระบุ");

        RuleFor(c => c)
            .CustomAsync(async (command, validatorContext, cancellationToken) =>
            {
                var entity = await context.Chatbots
                    .FirstOrDefaultAsync(x => x.Name == command.Name && x.Id != command.Id, cancellationToken);

                if (entity != null)
                {
                    validatorContext.AddFailure("ชื่อบอทซ้ำ");
                    return;
                }
            });

        RuleFor(v => v.LlmKey)
            .NotEmpty()
            .WithMessage("ModelHarbor API key ต้องไม่ว่าง");
    }
}
