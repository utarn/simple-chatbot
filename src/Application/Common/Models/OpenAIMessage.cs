using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class OpenAIMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    // Backing field for content
    private object _content;

    // Backward compatibility property
    [JsonIgnore]
    public string Content 
    { 
        get => _content as string;
        set => _content = value;
    }

    // Property to access content as object for complex types
    [JsonIgnore]
    public object ContentObject 
    { 
        get => _content;
        set => _content = value;
    }

    // This property will be used for JSON serialization
    [JsonPropertyName("content")]
    public object ContentForSerialization
    {
        get => _content;
        set => _content = value; // This setter is needed for deserialization
    }

    [JsonPropertyName("annotations")]
    public List<Annotation> Annotations { get; set; } = new List<Annotation>();

    // Helper methods for creating messages with different content types
    public static OpenAIMessage CreateTextMessage(string role, string content)
    {
        return new OpenAIMessage
        {
            Role = role,
            ContentObject = content
        };
    }

    public static OpenAIMessage CreateImageMessage(string role, string imageUrl)
    {
        return new OpenAIMessage
        {
            Role = role,
            ContentObject = new
            {
                image_url = new
                {
                    url = imageUrl
                }
            }
        };
    }

    public static OpenAIMessage CreateUserImageMessage(string imageUrl)
    {
        return CreateImageMessage("user", imageUrl);
    }
}