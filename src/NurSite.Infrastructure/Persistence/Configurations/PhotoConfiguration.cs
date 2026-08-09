using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

namespace NurSite.Infrastructure.Persistence.Configurations;

public class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> b)
    {
        b.Property(x => x.FilePath).HasMaxLength(400).IsRequired();
        b.Property(x => x.AltText).HasMaxLength(250).IsRequired();
        b.Property(x => x.Caption).HasMaxLength(500);
        b.HasIndex(x => new { x.AlbumId, x.SortOrder });

        b.HasOne(x => x.Album).WithMany(a => a.Photos)
         .HasForeignKey(x => x.AlbumId).OnDelete(DeleteBehavior.Cascade);
    }
}
