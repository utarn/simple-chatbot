using System.Numerics;
using System.Reflection;
using ChatbotApi.Domain.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChatbotApi.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Error> Errors => Set<Error>();
    public DbSet<ImportError> ImportErrors => Set<ImportError>();
    public DbSet<RefreshInformation> RefreshInformation => Set<RefreshInformation>();
    public DbSet<MessageHistory> MessageHistories => Set<MessageHistory>();
    public DbSet<Chatbot> Chatbots => Set<Chatbot>();
    public DbSet<PreMessage> PreMessages => Set<PreMessage>();
    public DbSet<FlexMessage> FlexMessages => Set<FlexMessage>();
    public DbSet<ChatbotPlugin> ChatbotPlugins => Set<ChatbotPlugin>();
    public DbSet<PreMessageContent> PreMessageContents => Set<PreMessageContent>();
    public DbSet<GmailSetting> GmailSettings => Set<GmailSetting>();
    public DbSet<CalendarSetting> CalendarSettings => Set<CalendarSetting>();
    public DbSet<TrackFile> TrackFiles => Set<TrackFile>();
    public DbSet<DriveSetting> DriveSettings => Set<DriveSetting>();
    public DbSet<IncomingRequest> IncomingRequests => Set<IncomingRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly(),
            type => type.Namespace == "ChatbotApi.Infrastructure.Data.Configurations");
        builder.HasPostgresExtension("vector");
    }


    public required DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
}
