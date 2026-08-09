using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

namespace NurSite.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> b)
    {
        b.Property(x => x.Title).HasMaxLength(250).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(250).IsRequired();
        b.Property(x => x.Summary).HasMaxLength(500);
        b.Property(x => x.TimeNote).HasMaxLength(120);
        b.Property(x => x.LocationName).HasMaxLength(200);
        b.Property(x => x.LocationAddress).HasMaxLength(400);
        b.Property(x => x.MetaTitle).HasMaxLength(70);
        b.Property(x => x.MetaDescription).HasMaxLength(170);
        b.Property(x => x.OgImagePath).HasMaxLength(400);
        b.Property(x => x.CoverImagePath).HasMaxLength(400);

        b.HasIndex(x => x.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasIndex(x => new { x.Status, x.StartsAtUtc });

        b.HasOne(x => x.Speaker).WithMany()
         .HasForeignKey(x => x.SpeakerId).OnDelete(DeleteBehavior.SetNull);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
