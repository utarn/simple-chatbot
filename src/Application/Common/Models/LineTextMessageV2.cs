using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class LineTextMessageV2 : LineMessage
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("substitution")]
    public Substitution Substitution { get; set; }

    public LineTextMessageV2()
    {
        Type = "textV2";
    }

    public LineTextMessageV2(string text, string user1, string? user2 = null, string? user3 = null)
    {
        Type = "textV2";
        Text = text;
        Substitution = new Substitution
        {
            User1 = new User1
            {
                Mentionee = new MentioneeReply
                {
                    UserId = user1
                }
            }
        };

        if (user2 != null)
        {
            Substitution.User2 = new User1
            {
                Mentionee = new MentioneeReply
                {
                    UserId = user2
                }
            };
        }

        if (user3 != null)
        {
            Substitution.User3 = new User1
            {
                Mentionee = new MentioneeReply
                {
                    UserId = user3
                }
            };
        }
    }
}