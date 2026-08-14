using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

namespace NurSite.Infrastructure.Persistence.Configurations;

public class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> b)
    {
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        b.Property(x => x.FilePath).HasMaxLength(400).IsRequired();
        b.Property(x => x.AltText).HasMaxLength(250).IsRequired();
        b.Property(x => x.Caption).HasMaxLength(500);
        b.Property(x => x.VideoPath).HasMaxLength(400);
        b.Property(x => x.ExternalVideoUrl).HasMaxLength(600);

        // این دو فقط راحتی کدند و ستون ندارند
        b.Ignore(x => x.VideoUrl);
        b.Ignore(x => x.HasVideo);
        b.Ignore(x => x.IsExternalVideo);

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.AlbumId, x.SortOrder });

        // فهرست گالری اغلب بر اساس نوع فیلتر می‌شود
        b.HasIndex(x => x.Kind);

        b.HasOne(x => x.Album).WithMany(a => a.Photos)
         .HasForeignKey(x => x.AlbumId).OnDelete(DeleteBehavior.Cascade);
    }
}