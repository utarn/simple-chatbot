using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ChatbotApi.Application.Webhook.Queries.GetFacebookSubscribeQuery;

public class GetFacebookSubscribeQuery : IRequest<IResult>
{
    public string Mode { get; set; }
    public string Challenge { get; set; }
    public string VerifyToken { get; set; }
    public int ChatbotId { get; set; }

    public class GetFacebookSubscribeQueryHandler : IRequestHandler<GetFacebookSubscribeQuery, IResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetFacebookSubscribeQueryHandler> _logger;

        public GetFacebookSubscribeQueryHandler(IApplicationDbContext context,
            ILogger<GetFacebookSubscribeQueryHandler> logger)
        {
            _context = context;
            _logger = logger;
        }


        public async Task<IResult> Handle(GetFacebookSubscribeQuery request, CancellationToken cancellationToken)
        {
            var chatbot =
                await _context.Chatbots.FirstOrDefaultAsync(c => c.Id == request.ChatbotId, cancellationToken);

            if (chatbot == null)
            {
                return Results.BadRequest();
            }
            
            if (request.Mode == "subscribe" && request.VerifyToken == chatbot.FacebookVerifyToken)
            {
                return Results.Ok(int.Parse(request.Challenge));
            }
            
            return Results.BadRequest();
            
        }
    }
}
