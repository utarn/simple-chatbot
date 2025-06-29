using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatbotApi.Infrastructure.Data.Configurations;

public class MessageHistoryConfiguration : IEntityTypeConfiguration<MessageHistory>
{
    public void Configure(EntityTypeBuilder<MessageHistory> builder)
    {
        builder.HasKey(b => new {b.ChatBotId, b.Created, b.UserId});
        builder.HasOne(b => b.ChatBot)
            .WithMany(b => b.MessageHistories)
            .HasForeignKey(b => b.ChatBotId);

        builder.HasIndex(b => b.Channel);
        builder.HasIndex(b => b.Created);
    }
}
