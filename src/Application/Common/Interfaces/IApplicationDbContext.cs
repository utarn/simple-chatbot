using Microsoft.AspNetCore.Identity;
using ChatbotApi.Domain.Entities;

namespace ChatbotApi.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<Error> Errors { get; }
    DbSet<ImportError> ImportErrors { get; }
    DbSet<RefreshInformation> RefreshInformation { get; }
    DbSet<MessageHistory> MessageHistories { get; }
    DbSet<Chatbot> Chatbots { get; }
    DbSet<PreMessage> PreMessages { get; }
    DbSet<FlexMessage> FlexMessages { get; }
    DbSet<ChatbotPlugin> ChatbotPlugins { get; }
    DbSet<PreMessageContent> PreMessageContents { get; }
    DbSet<GmailSetting> GmailSettings { get; }
    DbSet<CalendarSetting> CalendarSettings { get; }
    DbSet<DriveSetting> DriveSettings { get; }
    DbSet<TrackFile> TrackFiles { get; }
    DbSet<IncomingRequest> IncomingRequests { get; }
    DbSet<PlayList> PlayLists { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
