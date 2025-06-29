namespace ChatbotApi.Application.Chatbots.Queries.GetPreMessageQuery;

public class GetPreMessageQueryValidator : AbstractValidator<GetPreMessageQuery>
{
    public GetPreMessageQueryValidator(IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, cancellationToken) =>
            {
                return await context.Chatbots.AnyAsync(x => x.Id == id, cancellationToken);
            }).WithMessage("ไม่พบแชทบอทที่ระบุ");
    }
}
