using Utharn.Library.Localizer;

namespace ChatbotApi.Application.Chatbots.Queries.GetPluginByChatBotQuery;

public class PluginViewModel
{
    public int ChatbotId { get; set; }
    [Localize(Value = "รหัสปลั๊กอิน")]
    public string PluginName { get; set; } = default!;
    [Localize(Value = "คำอธิบาย")]
    public string Description { get; set; } = default!;
    [Localize(Value = "สถานะ")]
    public bool IsEnabled { get; set; }

    public static PluginViewModel MappingFunction(ChatbotApi.Domain.Entities.ChatbotPlugin plugin)
    {
        return new PluginViewModel
        {
            ChatbotId = plugin.ChatbotId,
            PluginName = plugin.PluginName,
            // Description and IsEnabled are not directly mapped from ChatbotPlugin
            // They are derived from Systems.Plugins and the presence in the ChatbotPlugins collection
            // This mapping function is primarily for direct ChatbotPlugin to PluginViewModel conversion if needed elsewhere.
        };
    }
}
