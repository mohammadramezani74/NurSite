using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

namespace NurSite.Infrastructure.Persistence.Configurations;

public class HeroVerseConfiguration : IEntityTypeConfiguration<HeroVerse>
{
    public void Configure(EntityTypeBuilder<HeroVerse> b)
    {
        b.Property(x => x.ArabicText).HasMaxLength(500).IsRequired();
        b.Property(x => x.PersianText).HasMaxLength(500).IsRequired();
        b.Property(x => x.Reference).HasMaxLength(150).IsRequired();
        b.HasIndex(x => new { x.IsActive, x.SortOrder });
    }
}
