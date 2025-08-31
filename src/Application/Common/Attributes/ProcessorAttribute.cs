using System;

namespace ChatbotApi.Application.Common.Attributes;

/// <summary>
/// Attribute to declare processor metadata (name and description) so discovery can read them
/// without instantiating the processor type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ProcessorAttribute : Attribute
{
    public string Name { get; }
    public string Description { get; }

    public ProcessorAttribute(string name, string description = "")
    {
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
    }
}