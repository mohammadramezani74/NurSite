using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

namespace NurSite.Infrastructure.Persistence.Configurations;

public class RulingSourceConfiguration : IEntityTypeConfiguration<RulingSource>
{
    public void Configure(EntityTypeBuilder<RulingSource> b)
    {
        b.Property(x => x.Title).HasMaxLength(250).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(250).IsRequired();
        b.Property(x => x.Author).HasMaxLength(200);
        b.Property(x => x.Editor).HasMaxLength(200);
        b.Property(x => x.Publisher).HasMaxLength(200);
        b.Property(x => x.Isbn).HasMaxLength(20);
        b.Property(x => x.Edition).HasMaxLength(100);
        b.Property(x => x.Url).HasMaxLength(400);
        b.Property(x => x.CoverImagePath).HasMaxLength(400);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.PermissionNote).HasMaxLength(1000);

        b.HasIndex(x => x.Slug).IsUnique();
    }
}
