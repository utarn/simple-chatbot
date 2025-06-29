using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatbotApi.Infrastructure.Data.Configurations;

public class GmailSettingConfiguration : IEntityTypeConfiguration<GmailSetting>
{
    public void Configure(EntityTypeBuilder<GmailSetting> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).UseIdentityAlwaysColumn();

        builder.Property(g => g.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(g => g.AccessToken)
            .IsRequired();

        builder.Property(g => g.RefreshToken)
            .IsRequired();

        builder.Property(g => g.CreatedAt)
            .IsRequired();

        builder.Property(g => g.UpdatedAt)
            .IsRequired();

        // Create unique index on UserId to ensure one record per user
        builder.HasIndex(g => g.UserId)
            .IsUnique();

        // Configure table name
        builder.ToTable("GmailSettings");
    }
}