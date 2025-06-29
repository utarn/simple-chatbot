namespace ChatbotApi.Application.Chatbots.Commands.DeleteFlexMessageCommand;

public class DeleteFlexMessageCommandValidator : AbstractValidator<DeleteFlexMessageCommand>
{
    public DeleteFlexMessageCommandValidator(IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        RuleFor(v => v.Id)
            .MustAsync(async (id, cancellationToken) =>
            {
                return await context.FlexMessages
                    .Include(c => c.Chatbot)
                    .AnyAsync(x => x.Id == id, cancellationToken);
            }).WithMessage("ไม่พบข้อความที่ระบุ");
    }
}
