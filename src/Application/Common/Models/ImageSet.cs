using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class ImageSet
{
    /// <summary>
    /// Gets or sets the unique identifier for the image set.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the index of the current image in the set.
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the total number of images in the set.
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }
}
