namespace ChatbotApi.Application.Chatbots.Queries.GetPreMessageByIdQuery;

public class GetPreMessageByIdQueryValidator : AbstractValidator<GetPreMessageByIdQuery>
{
    public GetPreMessageByIdQueryValidator(IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, cancellationToken) =>
            {
                return await context.Chatbots.AnyAsync(x => x.Id == id, cancellationToken);
            }).WithMessage("ไม่พบแชทบอทที่ระบุ");

        RuleFor(x => x.Order)
            .MustAsync(async (command, order, cancellationToken) =>
            {
                return await context.PreMessages.AnyAsync(x => x.Order == order && x.ChatBotId == command.Id,
                    cancellationToken);
            }).WithMessage("ไม่พบความรู้ของบอทนี้");
    }
}
