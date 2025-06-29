namespace ChatbotApi.Application.Chatbots.Commands.EditPreMessageCommand;

public class EditPreMessageCommandValidator : AbstractValidator<EditPreMessageCommand>
{
    public EditPreMessageCommandValidator(IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, cancellationToken) =>
            {
                return await context.Chatbots
                    .AnyAsync(x => x.Id == id, cancellationToken);
            }).WithMessage("ไม่พบแชทบอทที่ระบุ");

        RuleFor(x => x.Order)
            .MustAsync(async (command, order, cancellationToken) =>
            {
                return await context.PreMessages.AnyAsync(x => x.Order == order && x.ChatBotId == command.Id,
                    cancellationToken);
            }).WithMessage("ไม่พบความรู้ของบอทนี้");

        RuleFor(x => x.UserMessage)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("กรุณากรอกข้อความของผู้ใช้งาน")
            .MaximumLength(8192)
            .WithMessage("ข้อความของผู้ใช้งานยาวเกินไป");

        // RuleFor(x => x.AssistantMessage)
        //     .Cascade(CascadeMode.Stop)
        //     .NotEmpty()
        //     .WithMessage("กรุณากรอกข้อความของผู้ช่วย")
        //     .MaximumLength(8000)
        //     .WithMessage("ข้อความของผู้ช่วยยาวเกินไป");
    }
}
