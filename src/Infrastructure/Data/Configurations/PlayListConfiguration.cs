using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatbotApi.Infrastructure.Data.Configurations;

public class PlayListConfiguration : IEntityTypeConfiguration<PlayList>
{
    public void Configure(EntityTypeBuilder<PlayList> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseIdentityAlwaysColumn();

        builder.Property(p => p.MusicName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.AlbumName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.CreatedDate)
            .IsRequired();
    }
}