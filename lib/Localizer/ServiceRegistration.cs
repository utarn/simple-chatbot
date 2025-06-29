using Microsoft.Extensions.DependencyInjection;

namespace Utharn.Library.Localizer;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterLocalization(this IServiceCollection services)
    {
        services.AddScoped<ILocalizerService, LocalizerService>();
        return services;
    }
}