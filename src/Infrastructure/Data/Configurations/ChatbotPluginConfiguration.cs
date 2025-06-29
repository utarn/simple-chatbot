using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatbotApi.Infrastructure.Data.Configurations;

public class ChatbotPluginConfiguration : IEntityTypeConfiguration<ChatbotPlugin>
{
    public void Configure(EntityTypeBuilder<ChatbotPlugin> builder)
    {
        builder.HasKey(b => new {b.ChatbotId, b.PluginName});
        builder.HasOne(b => b.Chatbot)
            .WithMany(b => b.ChatbotPlugins)
            .HasForeignKey(b => b.ChatbotId);
    }
}
