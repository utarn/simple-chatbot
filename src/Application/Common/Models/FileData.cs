using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class FileData
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; }
    
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }
}
