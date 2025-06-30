
using System.Globalization;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Application.Webhook.Commands.LineWebhookCommand;

namespace ChatbotApi.Application.Chatbots.Commands.ObtainAssistantMessageCommand;

public class ObtainAssistantMessageCommand : IRequest<string>
{
    public int Id { get; set; }
    public int Order { get; set; }

    public class ObtainAssistantMessageCommandHandler : IRequestHandler<ObtainAssistantMessageCommand, string>
    {
        private readonly IApplicationDbContext _context;
        private readonly ISystemService _systemService;
        private readonly IOpenAiService _openAiService;

        public ObtainAssistantMessageCommandHandler(IApplicationDbContext context, ISystemService systemService,
            IOpenAiService openAiService)
        {
            _context = context;
            _systemService = systemService;
            _openAiService = openAiService;
        }

        public async Task<string> Handle(
            ObtainAssistantMessageCommand request, CancellationToken cancellationToken)
        {
            var chatbot = await _context.Chatbots
                .Include(x => x.PredefineMessages)
                .FirstAsync(x => x.Id == request.Id, cancellationToken);

            if (chatbot.LlmKey == null)
            {
                return string.Empty;
            }

            var toSendMessage = new List<OpenAIMessage>();
            if (chatbot.SystemRole != null)
            {
                toSendMessage.Add(new OpenAIMessage() { Role = "system", Content = chatbot.SystemRole });
            }

            foreach (var premessage in chatbot.PredefineMessages.OrderBy(p => p.Order))
            {
                var userMessage = premessage.UserMessage.Replace("%DATE%",
                    _systemService.Now.ToString("d MMMM yyyy HH:mm:ssน.", CultureInfo.GetCultureInfo("th-TH")));
                var assistantMessage = premessage.AssistantMessage?.Replace("%DATE%",
                    _systemService.Now.ToString("d MMMM yyyy HH:mm:ssน.", CultureInfo.GetCultureInfo("th-TH")));
                toSendMessage.Add(new OpenAIMessage() { Role = "user", Content = userMessage });
                if (assistantMessage != null)
                {
                    toSendMessage.Add(new OpenAIMessage() { Role = "assistant", Content = assistantMessage });
                }
            }

            var apiResponse =
                await _openAiService.GetOpenAiResponseAsync(new OpenAiRequest() { Messages = toSendMessage },
                    chatbot.LlmKey,
                    cancellationToken);

            if (apiResponse is not { Choices.Count: > 0 })
            {
                return string.Empty;
            }

            var aiResponseText = apiResponse.Choices[0].Message.Content;

            var toUpdatePreMessage = await _context.PreMessages
                .FirstAsync(x => x.ChatBotId == request.Id && x.Order == request.Order, cancellationToken);

            toUpdatePreMessage.AssistantMessage = aiResponseText;
            await _context.SaveChangesAsync(cancellationToken);
            return aiResponseText;
        }
    }
}
