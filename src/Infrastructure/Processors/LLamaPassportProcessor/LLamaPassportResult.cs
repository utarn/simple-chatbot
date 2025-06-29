using System.Text.Json.Serialization;

namespace ChatbotApi.Infrastructure.Processors.LLamaPassportProcessor;

public class LLamaPassportResult // Make public if used outside this file
{
    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    // [JsonPropertyName("type")]
    // public string? Type { get; set; }
    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; set; }

    // [JsonPropertyName("dateOfIssue")]
    // public string? DateOfIssue { get; set; }

    // [JsonPropertyName("dateOfExpiry")]
    // public string? DateOfExpiry { get; set; }

    // [JsonPropertyName("placeOfBirth")]
    // public string? PlaceOfBirth { get; set; }

    [JsonPropertyName("nationality")]
    public string? Nationality { get; set; }

    [JsonPropertyName("passportNumber")]
    public string? PassportNumber { get; set; }

    [JsonPropertyName("sex")]
    public string? Sex { get; set; }
    
    [JsonPropertyName("address")]
    public string? Address { get; set; }
    
    [JsonPropertyName("telephone")]
    public string? Telephone { get; set; }
}


public class AddressResult
{
    [JsonPropertyName("address")]
    public string? Address { get; set; }
    [JsonPropertyName("telephone")]
    public string? Telephone { get; set; }
}
