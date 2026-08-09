using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

public class RulingConfiguration : IEntityTypeConfiguration<Ruling>
{
    public void Configure(EntityTypeBuilder<Ruling> b)
    {
        b.Property(x => x.Question).HasMaxLength(400).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(250).IsRequired();
        b.Property(x => x.FatwaNote).HasMaxLength(250);
        b.Property(x => x.SourceReference).HasMaxLength(400);
        b.Property(x => x.MetaTitle).HasMaxLength(70);
        b.Property(x => x.MetaDescription).HasMaxLength(170);
        b.Property(x => x.OgImagePath).HasMaxLength(400);

        b.HasIndex(x => x.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasIndex(x => new { x.RulingCategoryId, x.SortOrder });
        b.HasIndex(x => x.IsFrequentlyAsked);

        b.HasOne(x => x.RulingCategory)
         .WithMany(c => c.Rulings)
         .HasForeignKey(x => x.RulingCategoryId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Marja)
         .WithMany(m => m.Rulings)
         .HasForeignKey(x => x.MarjaId)
         .OnDelete(DeleteBehavior.SetNull);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
