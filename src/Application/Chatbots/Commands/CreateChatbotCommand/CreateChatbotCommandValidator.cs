namespace ChatbotApi.Application.Chatbots.Commands.CreateChatbotCommand;

public class CreateChatbotCommandValidator : AbstractValidator<CreateChatbotCommand>
{
    public CreateChatbotCommandValidator(IApplicationDbContext context)
    {
        RuleFor(v => v.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("ชื่อบอทต้องไม่ว่าง")
            .CustomAsync(async (name, validatorContext, cancellationToken) =>
            {
                var entity = await context.Chatbots
                    .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);

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
