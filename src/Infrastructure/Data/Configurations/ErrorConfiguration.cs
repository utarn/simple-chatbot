using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.IdentityModel.Abstractions;

namespace ChatbotApi.Infrastructure.Data.Configurations;

public class ErrorConfiguration : IEntityTypeConfiguration<Error>
{
    public void Configure(EntityTypeBuilder<Error> builder)
    {
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.ChatBotId, b.Created });
        
        builder.HasDiscriminator<string>(b => b.Type)
            .HasValue<ImportError>("import_error");
    }
}

public class ImportErrorConfiguration : IEntityTypeConfiguration<ImportError>
{
    public void Configure(EntityTypeBuilder<ImportError> builder)
    {
        builder.HasOne(b => b.ChatBot)
            .WithMany(b => b.ImportErrors)
            .HasForeignKey(b => b.ChatBotId);
        
        
    }
}
