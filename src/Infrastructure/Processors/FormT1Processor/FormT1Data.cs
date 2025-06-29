// src/Infrastructure/Processors/FormT1Processor/FormT1Data.cs

using System.Text.Json.Serialization;

namespace ChatbotApi.Infrastructure.Processors.FormT1Processor;

public class FormT1Data
{
    [JsonPropertyName("need_to_create")]
    public bool NeedToCreate { get; set; }
    [JsonPropertyName("write_current_date")]
    public bool WriteCurrentDate { get; set; }
    
    [JsonPropertyName("write_at")]
    public string WriteAt { get; set; } = string.Empty;

    [JsonPropertyName("story")]
    public string Story { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty;

    [JsonPropertyName("faculty")]
    public string Faculty { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("from_date")]
    public string FromDate { get; set; } = string.Empty;

    [JsonPropertyName("to_date")]
    public string ToDate { get; set; } = string.Empty;
    
    [JsonPropertyName("telephone")]
    public string Telephone { get; set; } = string.Empty;
}
