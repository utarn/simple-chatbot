using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatbotApi.Infrastructure.Data.Configurations;

public class DriveSettingConfiguration : IEntityTypeConfiguration<DriveSetting>
{
    public void Configure(EntityTypeBuilder<DriveSetting> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).UseIdentityAlwaysColumn();

        builder.Property(d => d.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(d => d.AccessToken)
            .IsRequired();

        builder.Property(d => d.RefreshToken)
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .IsRequired();

        // Create unique index on UserId to ensure one record per user
        builder.HasIndex(d => d.UserId)
            .IsUnique();

        // Configure table name
        builder.ToTable("DriveSettings");
    }
}