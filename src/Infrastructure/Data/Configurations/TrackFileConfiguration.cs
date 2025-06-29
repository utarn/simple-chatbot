using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;

namespace ChatbotApi.Infrastructure.Data.Configurations;

public class TrackFileConfiguration : IEntityTypeConfiguration<TrackFile>
{
    public void Configure(EntityTypeBuilder<TrackFile> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.FilePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.ContentType)
            .HasMaxLength(100);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.FileHash)
            .HasMaxLength(64);

        builder.Property(x => x.Embedding)
            .HasColumnType("vector(1024)");

        // Create vector index for similarity search
        builder.HasIndex(x => x.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_l2_ops")
            .HasStorageParameter("ef_construction", 64)
            .HasStorageParameter("m", 16);

        // Create indexes for common queries
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ChatbotId);
        builder.HasIndex(x => x.FileHash);
        builder.HasIndex(x => new { x.UserId, x.Created });

        // Configure relationship with Chatbot
        builder.HasOne(x => x.Chatbot)
            .WithMany()
            .HasForeignKey(x => x.ChatbotId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
