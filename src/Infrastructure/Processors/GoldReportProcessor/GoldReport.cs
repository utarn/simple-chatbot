using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace ChatbotApi.Infrastructure.Processors.GoldReportProcessor
{
    public class GoldReport
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("goldPurity")]
        public string? GoldPurity { get; set; }

        [JsonPropertyName("prices")]
        public Prices? Prices { get; set; }

        [JsonPropertyName("dailyChange")]
        public DailyChange? DailyChange { get; set; }

        [JsonPropertyName("updateDetails")]
        public UpdateDetails? UpdateDetails { get; set; }
    }

    public class Prices
    {
        [JsonPropertyName("goldBar")]
        public GoldType? GoldBar { get; set; }

        [JsonPropertyName("ornamentalGold")]
        public GoldType? OrnamentalGold { get; set; }
    }

    public class GoldType
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("bidPrice")]
        public decimal BidPrice { get; set; }

        [JsonPropertyName("askPrice")]
        public decimal AskPrice { get; set; }
    }

    public class DailyChange
    {
        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("direction")]
        public string? Direction { get; set; }
    }

    public class UpdateDetails
    {
        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("time")]
        public string? Time { get; set; }

        [JsonPropertyName("revision")]
        public int Revision { get; set; }
    }
}