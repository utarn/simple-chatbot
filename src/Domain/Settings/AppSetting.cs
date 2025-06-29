namespace ChatbotApi.Domain.Settings;

public class AppSetting
{
    public string AppName { get; set; } = "ModelHarbor Chatbot";
    public string Organization { get; set; } = "ModelHarbor";
    public string Wallpaper { get; set; } = "https://www.modelharbor.com/assets/img/banner1.png";
    public string Logo { get; set; } = "https://www.modelharbor.com/assets/modelharbor.png";

    public string CopyRight { get; set; } = "ModelHarbor © 2025";
    public string Website { get; set; } = "https://www.modelharbor.com";
    public string FootNote { get; set; } = "ModelHarbor is a platform for building AI applications with ease. It provides tools and resources to help developers create, deploy, and manage AI models effectively.";
    public string AdminPassword { get; set; } = "changeme";
}
