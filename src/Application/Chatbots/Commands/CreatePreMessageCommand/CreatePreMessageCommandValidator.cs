namespace ChatbotApi.Application.Chatbots.Commands.CreatePreMessageCommand;

public class CreatePreMessageCommandValidator : AbstractValidator<CreatePreMessageCommand>
{
    public CreatePreMessageCommandValidator(IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, cancellationToken) =>
            {
                return await context.Chatbots
                    .AnyAsync(x => x.Id == id,
                        cancellationToken);
            }).WithMessage("ไม่พบแชทบอทที่ระบุ");

        RuleFor(x => x.Order)
            .Cascade(CascadeMode.Stop)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Order ต้องมากกว่าหรือเท่ากับ 1")
            .LessThanOrEqualTo(10000)
            .WithMessage("Order ต้องน้อยกว่าหรือเท่ากับ 10000")
            .MustAsync(async (command, order, cancellationToken) =>
            {
                return !await context.PreMessages.AnyAsync(x => x.Order == order && x.ChatBotId == command.Id,
                    cancellationToken);
            }).WithMessage("Order ซ้ำในแชทบอทเดียวกัน");

        RuleFor(x => x.UserMessage)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("กรุณากรอกข้อความของผู้ใช้งาน")
            .MaximumLength(8192)
            .WithMessage("ข้อความของผู้ใช้งานยาวเกินไป");
    }
}
