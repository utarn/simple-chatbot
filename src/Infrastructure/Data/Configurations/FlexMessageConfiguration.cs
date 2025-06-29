using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatbotApi.Infrastructure.Data.Configurations;

public class FlexMessageConfiguration : IEntityTypeConfiguration<FlexMessage>
{
    public void Configure(EntityTypeBuilder<FlexMessage> builder)
    {
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.ChatbotId, b.Type });

        builder.HasOne(b => b.Chatbot)
            .WithMany(b => b.FlexMessages)
            .HasForeignKey(b => b.ChatbotId);
    }
}
