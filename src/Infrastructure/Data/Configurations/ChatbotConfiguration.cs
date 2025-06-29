using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatbotApi.Infrastructure.Data.Configurations;

public class ChatbotConfiguration : IEntityTypeConfiguration<Chatbot>
{
    public void Configure(EntityTypeBuilder<Chatbot> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).UseIdentityAlwaysColumn();
    }
}
