using System;

namespace Utharn.Library.Localizer;

[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Method |
    AttributeTargets.Class,
    AllowMultiple = true)]
public sealed class LocalizeAttribute : Attribute
{
    public const string Thai = "th-TH";
    public const string English = "en-US";

    public LocalizeAttribute()
    {
    }

    public LocalizeAttribute(string value, string language = "Thai")
    {
        Language = language;
        Value = value;
    }

    public string Language { get; set; } = Thai;
    public string Value { get; set; } = default!;
    public string Description { get; set; } = default!;
}