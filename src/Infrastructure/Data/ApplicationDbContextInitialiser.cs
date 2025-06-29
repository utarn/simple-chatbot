using ChatbotApi.Domain.Entities;
using ChatbotApi.Domain.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChatbotApi.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
        await initializer.InitialiseAsync();
        await initializer.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppSetting _appSetting;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        AppSetting appSetting)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _appSetting = appSetting;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task TrySeedAsync()
    {
        if (!_userManager.Users.Any())
        {
            var adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@localhost", // placeholder, not used for login
                Name = "Administrator",
                EmailConfirmed = true,
                IsEnabled = true,
                SendEmail = false
            };

            var result = await _userManager.CreateAsync(adminUser, _appSetting.AdminPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation("Admin user created successfully");
            }
            else
            {
                _logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        await _context.SaveChangesAsync();
    }

}
