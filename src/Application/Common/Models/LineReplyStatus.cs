using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class LineReplyStatus
{
    public int Status { get; set; }
    public string? Error { get; set; }
    public LineReplyMessage? ReplyMessage { get; set; }
    public string? Raw { get; set; }
    public List<ContentResult> ContentResults { get; set; } = new List<ContentResult>();

    public static LineReplyStatus SplitLineTextMessages(LineReplyStatus lineReplyStatus)
    {
        if (lineReplyStatus.ReplyMessage?.Messages == null || lineReplyStatus.ReplyMessage.Messages.Count == 0)
        {
            return lineReplyStatus;
        }

        var newMessages = new List<LineMessage>();

        foreach (var message in lineReplyStatus.ReplyMessage.Messages)
        {
            if (message is LineTextMessage textMessage && textMessage.Text.Length > 5000)
            {
                var textArray = SplitWithOverlap(textMessage.Text);
                foreach (var text in textArray)
                {
                    newMessages.Add(new LineTextMessage(text));
                }
            }
            else
            {
                newMessages.Add(message);
            }
        }

        return new LineReplyStatus
        {
            Status = lineReplyStatus.Status,
            ReplyMessage = new LineReplyMessage
            {
                ReplyToken = lineReplyStatus.ReplyMessage.ReplyToken,
                Messages = newMessages
            }
        };
    }

    private static List<string> SplitWithOverlap(string text, int maxChunkSize = 5000, int overlap = 50)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new List<string>();
        }

        if (maxChunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxChunkSize), "Max chunk size must be greater than zero.");
        }

        if (overlap < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(overlap), "Overlap cannot be negative.");
        }

        if (overlap >= maxChunkSize)
        {
            throw new ArgumentOutOfRangeException(nameof(overlap), "Overlap cannot be greater than or equal to the max chunk size.");
        }

        List<string> chunks = new List<string>();
        int startIndex = 0;

        while (startIndex < text.Length)
        {
            int endIndex = Math.Min(startIndex + maxChunkSize, text.Length);
            int length = endIndex - startIndex;
            chunks.Add(text.Substring(startIndex, length));

            if (length == maxChunkSize)
            {
                startIndex = endIndex - overlap;
            }
            else
            {
                startIndex = endIndex;
            }

            if (startIndex < 0)
            {
                startIndex = endIndex;
            }
        }

        return chunks;
    }
}