using System.Text.Json.Serialization;

namespace ChatbotApi.Infrastructure.Processors.CheckCheatOnlineProcessor;

public class CheckGonCaseData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("caseId")]
    public string? CaseId { get; set; }

    // Add other relevant fields you might want to display
    [JsonPropertyName("dataSource")]
    public string? DataSource { get; set; }

    [JsonPropertyName("suspectFullname")]
    public string? SuspectFullname { get; set; }

    [JsonPropertyName("suspectBankAccountNo")]
    public string? SuspectBankAccountNo { get; set; }

    [JsonPropertyName("suspectPhoneNumber")]
    public string? SuspectPhoneNumber { get; set; }

    [JsonPropertyName("fakeUrl")]
    public string? FakeUrl { get; set; } // Use this for website checks

    [JsonPropertyName("realUrl")]
    public string? RealUrl { get; set; }

    [JsonPropertyName("caseSeverity")]
    public string? CaseSeverity { get; set; }

    [JsonPropertyName("damagePrice")]
    public decimal? DamagePrice { get; set; } // Use decimal for currency

    [JsonPropertyName("caseType")]
    public string? CaseType { get; set; } // e.g., "website", "phone", "bank_account"

    [JsonPropertyName("cheatType")]
    public string? CheatType { get; set; }

    [JsonPropertyName("informDate")]
    public DateTime? InformDate { get; set; } // Use DateTime?

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("reportType")]
    public string? ReportType { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    // Note: Add JsonPropertyName attribute for all properties
    // if your C# naming convention differs from the JSON key.
}
