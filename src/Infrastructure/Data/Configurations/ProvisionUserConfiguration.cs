using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatbotApi.Infrastructure.Data.Configurations;

public class ProvisionUserConfiguration : IEntityTypeConfiguration<ProvisionUser>
{
    public void Configure(EntityTypeBuilder<ProvisionUser> builder)
    {
        builder.HasKey(b => b.Email);
        builder.Property(b => b.Email).ValueGeneratedNever();
    }
}
