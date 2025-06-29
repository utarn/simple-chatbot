using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatbotApi.Infrastructure.Data.Configurations;

public class PreMessageConfiguration : IEntityTypeConfiguration<PreMessage>
{
    public void Configure(EntityTypeBuilder<PreMessage> builder)
    {
        builder.HasKey(b => new {b.ChatBotId, b.Order});
        builder.HasOne(b => b.ChatBot)
            .WithMany(b => b.PredefineMessages)
            .HasForeignKey(b => b.ChatBotId);
        
        builder.Property(b => b.Embedding)
            .HasColumnType("vector(1024)");
        
        builder.HasIndex(i => i.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_l2_ops")
            .HasStorageParameter("m", 16)
            .HasStorageParameter("ef_construction", 64);
            
        builder.HasOne(b => b.PreMessageContent)
            .WithMany(b => b.PreMessages)
            .HasForeignKey(b => b.PreMessageContentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


public class PreMessageContentConfiguration : IEntityTypeConfiguration<PreMessageContent>
{
    public void Configure(EntityTypeBuilder<PreMessageContent> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Content)
            .HasColumnType("bytea");
    }
}
