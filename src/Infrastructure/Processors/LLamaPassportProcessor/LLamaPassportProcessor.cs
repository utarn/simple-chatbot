using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Application.Common.Attributes;
 
namespace ChatbotApi.Infrastructure.Processors.LLamaPassportProcessor;
 
[Processor("LlamaPassport","Llama Passport (Line)")]
public class LLamaPassportProcessor : ILineMessageProcessor
{
    public Task<LineReplyStatus> ProcessLineAsync(LineEvent evt, int chatbotId, string message, string userId,
        string replyToken, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LineReplyStatus
        {
            Status = 200,
            ReplyMessage = new LineReplyMessage
            {
                ReplyToken = replyToken,
                Messages = new List<LineMessage>
                {
                    new LineTextMessage("Passport processor is under maintenance")
                }
            }
        });
    }

    public Task<LineReplyStatus> ProcessLineImageAsync(LineEvent evt, int chatbotId, string messageId,
        string userId, string replyToken, string accessToken, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LineReplyStatus
        {
            Status = 200,
            ReplyMessage = new LineReplyMessage
            {
                ReplyToken = replyToken,
                Messages = new List<LineMessage>
                {
                    new LineTextMessage("Image processing for passport is under maintenance")
                }
            }
        });
    }

    public Task<LineReplyStatus> ProcessLineImagesAsync(LineEvent mainEvent, int chatbotId, List<LineEvent> imageEvents,
        string userId, string replyToken, string accessToken, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LineReplyStatus
        {
            Status = 200,
            ReplyMessage = new LineReplyMessage
            {
                ReplyToken = replyToken,
                Messages = new List<LineMessage>
                {
                    new LineTextMessage("Multiple image processing for passport is under maintenance")
                }
            }
        });
    }
}