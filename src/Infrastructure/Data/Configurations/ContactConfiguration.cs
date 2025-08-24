using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatbotApi.Infrastructure.Data.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(50);

        builder.Property(x => x.Email)
            .HasMaxLength(100);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(450);

        // Create indexes for common queries
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.PhoneNumber);
    }
}