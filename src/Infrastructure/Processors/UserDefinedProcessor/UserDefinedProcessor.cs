using System.Runtime.CompilerServices;
using ChatbotApi.Application.Common.Attributes;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Domain.Constants;
using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatbotApi.Infrastructure.Processors.UserDefinedProcessor;

[Processor("CustomJSON", "เปิดการใช้งาน Line Flex หรือ Custom JSON (Line, GoogleChat)")]
public class UserDefinedProcessor : ILineMessageProcessor
{
    private readonly IApplicationDbContext _context;

    public UserDefinedProcessor(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LineReplyStatus> ProcessLineAsync(LineEvent evt, int chatbotId, string message, string userId, string replyToken,
        CancellationToken cancellationToken = default)
    {
        ConfiguredCancelableAsyncEnumerable<FlexMessage> flexMessage = _context.FlexMessages
            .Where(f => f.ChatbotId == chatbotId && f.Type == "line")
            .OrderBy(f => f.Order)
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken);

        string? checkingMessage = message.ToLower().Trim();
        await foreach (FlexMessage? flex in flexMessage)
        {
            if (checkingMessage == flex.Key)
            {
                return new LineReplyStatus { Status = 201, Raw = flex.JsonValue };
            }
        }

        return new LineReplyStatus { Status = 404 };
    }

    public Task<LineReplyStatus> ProcessLineImageAsync(LineEvent evt, int chatbotId, string messageId, string userId,
        string replyToken, string accessToken,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LineReplyStatus { Status = 404 });
    }
}
