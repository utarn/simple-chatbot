using Utharn.Library.Localizer;

namespace ChatbotApi.Application.Chatbots.Queries.GetChatbotQuery;

public class ChatbotViewModel
{
    public int Id { get; set; }
    [Localize(Value = "ชื่อบอท")]
    public string Name { get; set; } = default!;

    public bool CanDelete { get; set; }
    public string? GoogleChatServiceAccount { get; set; }
    public string? FacebookVerifyToken { get; set; }
    public string? FacebookAccessToken { get; set; }
    public string? LineChannelAccessToken { get; set; }
    public string? ProtectedApiKey { get; set; }

    public List<string> Plugins { get; set; } = new List<string>();
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Domain.Entities.Chatbot, ChatbotViewModel>()
                .ForMember(d => d.CanDelete, opt => opt.MapFrom(
                    s => s.MessageHistories.Count == 0 && s.FlexMessages.Count == 0))
                .ForMember(d => d.Plugins, opt => opt.MapFrom(
                    s => s.ChatbotPlugins.Select(x => x.PluginName).ToList()))
                ;
        }
    }
}
