namespace ChatbotApi.Application.Chatbots.Commands.EditFlexMessageCommand;

public class EditFlexMessageCommandValidator : AbstractValidator<EditFlexMessageCommand>
{
    public EditFlexMessageCommandValidator(IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        RuleFor(v => v)
            .MustAsync(async (command, cancellationToken) =>
            {
                return await context.FlexMessages.AnyAsync(f => f.Id == command.Id && f.ChatbotId == command.ChatbotId,
                    cancellationToken);
            })
            .WithMessage("ไม่พบข้อความที่ระบุในแชทบอทที่ระบุ")
            ;


        RuleFor(v => v.Id)
            .MustAsync(async (id, cancellationToken) =>
            {
                return await context.FlexMessages
                    .Include(c => c.Chatbot)
                    .AnyAsync(x => x.Id == id, cancellationToken);
            }).WithMessage("ไม่พบข้อความที่ระบุ");

        RuleFor(x => x.Type)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("กรุณากรอกช่องทางบริการ")
            .Must(x => x is "line" or "google")
            .WithMessage("ช่องทางบริการต้องเป็น line หรือ google");

        RuleFor(x => x.Key)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("กรุณากรอกข้อความหรือ Postback ที่ต้องการส่ง (ภาษาอังกฤษใช้ตัวอักษรเล็กเท่านั้น)")
            .MaximumLength(100)
            .WithMessage("ข้อความหรือ Postback ที่ต้องการส่ง (ภาษาอังกฤษใช้ตัวอักษรเล็กเท่านั้น)ยาวเกินไป");

        RuleFor(x => x.Order)
            .Cascade(CascadeMode.Stop)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Order ต้องมากกว่าหรือเท่ากับ 1")
            .LessThanOrEqualTo(100)
            .WithMessage("Order ต้องน้อยกว่าหรือเท่ากับ 100");

        RuleFor(x => x.JsonValue)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("กรุณากรอกข้อความตอบกลับ")
            .MaximumLength(8000)
            .WithMessage("ข้อความตอบกลับยาวเกินไป");
    }
}
