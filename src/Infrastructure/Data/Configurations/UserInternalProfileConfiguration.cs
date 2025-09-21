using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatbotApi.Infrastructure.Data.Configurations;

public class UserInternalProfileConfiguration : IEntityTypeConfiguration<UserInternalProfile>
{
    public void Configure(EntityTypeBuilder<UserInternalProfile> builder)
    {
        builder.Property(e => e.LineUserId)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(e => e.Initial)
            .HasMaxLength(10)
            .IsRequired(false);

        builder.Property(e => e.FirstName)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(e => e.LastName)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(e => e.Group)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(e => e.Faculty)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(e => e.Campus)
            .HasMaxLength(100)
            .IsRequired(false);

        // Index on LineUserId for faster lookups
        builder.HasIndex(e => e.LineUserId)
            .IsUnique();
    }
}