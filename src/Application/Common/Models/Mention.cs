using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class Mention
{
    [JsonPropertyName("mentionees")]
    public List<Mentionee> Mentionees { get; set; }
}