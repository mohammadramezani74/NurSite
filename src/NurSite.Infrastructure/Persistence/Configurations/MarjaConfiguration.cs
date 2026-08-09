using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

public class MarjaConfiguration : IEntityTypeConfiguration<Marja>
{
    public void Configure(EntityTypeBuilder<Marja> b)
    {
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(150).IsRequired();
        b.Property(x => x.OfficialSiteUrl).HasMaxLength(300);
        b.Property(x => x.PortraitPath).HasMaxLength(400);
        b.HasIndex(x => x.Slug).IsUnique();
    }
}
