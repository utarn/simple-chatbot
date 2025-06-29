using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatbotApi.Infrastructure.Data.Configurations;

public class IncomingRequestConfiguration : IEntityTypeConfiguration<IncomingRequest>
{
    public void Configure(EntityTypeBuilder<IncomingRequest> builder)
    {
        builder.ToTable("IncomingRequest");
    }
}
