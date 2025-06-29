namespace ChatbotApi.Application.Common.Exceptions;

public class ChatCompletionException : Exception
{
    public int StatusCode { get; }

    public ChatCompletionException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
