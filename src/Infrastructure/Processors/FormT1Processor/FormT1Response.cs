// src/Infrastructure/Processors/FormT1Processor/FormT1Data.cs

using System.Text.Json.Serialization;

namespace ChatbotApi.Infrastructure.Processors.FormT1Processor;

public class FormT1Response
{
    [JsonPropertyName("result")]
    public FormT1Data Result { get; set; } = new FormT1Data();
}
