using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

namespace NurSite.Infrastructure.Persistence.Configurations;

public class LectureConfiguration : IEntityTypeConfiguration<Lecture>
{
    public void Configure(EntityTypeBuilder<Lecture> b)
    {
        b.Property(x => x.Title).HasMaxLength(250).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(250).IsRequired();
        b.Property(x => x.AudioPath).HasMaxLength(400).IsRequired();
        b.Property(x => x.MetaTitle).HasMaxLength(70);
        b.Property(x => x.MetaDescription).HasMaxLength(170);
        b.Property(x => x.OgImagePath).HasMaxLength(400);

        b.HasIndex(x => x.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasIndex(x => new { x.Status, x.PublishedAtUtc });

        b.HasOne(x => x.Speaker).WithMany(s => s.Lectures)
         .HasForeignKey(x => x.SpeakerId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.LectureSeries).WithMany(s => s.Lectures)
         .HasForeignKey(x => x.LectureSeriesId).OnDelete(DeleteBehavior.SetNull);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
