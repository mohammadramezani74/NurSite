using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

namespace NurSite.Infrastructure.Persistence.Configurations;

public class OccasionConfiguration : IEntityTypeConfiguration<Occasion>
{
    public void Configure(EntityTypeBuilder<Occasion> b)
    {
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.HijriMonth, x.HijriDay });
        b.Property(x => x.Description).HasMaxLength(1000);

        // هر دو محدودیت باید در یک فراخوانی ToTable تعریف شوند
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Occasion_HijriMonth", "[HijriMonth] BETWEEN 1 AND 12");
            t.HasCheckConstraint("CK_Occasion_HijriDay", "[HijriDay] BETWEEN 1 AND 30");
        });
    }
}
