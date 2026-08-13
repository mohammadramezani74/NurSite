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

        // اجباری نیست، چون صوت می‌تواند روی سرور دیگری باشد.
        // اینکه دست‌کم یکی از این دو پر باشد، در فرم بررسی می‌شود.
        b.Property(x => x.AudioPath).HasMaxLength(400);
        b.Property(x => x.ExternalAudioUrl).HasMaxLength(600);

        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.SearchText).HasColumnType("nvarchar(max)");
        b.Property(x => x.MetaTitle).HasMaxLength(70);
        b.Property(x => x.MetaDescription).HasMaxLength(170);
        b.Property(x => x.OgImagePath).HasMaxLength(400);

        // این دو فقط راحتی کدند و ستون ندارند
        b.Ignore(x => x.AudioUrl);
        b.Ignore(x => x.IsExternal);

        b.HasIndex(x => x.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasIndex(x => new { x.Status, x.PublishedAtUtc });

        // فهرست هر بخش عمومی همیشه بر اساس نوع فیلتر می‌شود، پس نوع
        // باید ستون اول ایندکس باشد وگرنه ایندکس بالا به کارش نمی‌آید
        b.HasIndex(x => new { x.Kind, x.Status, x.PublishedAtUtc });

        // فهرست هر مجموعه به ترتیب جلسه خوانده می‌شود
        b.HasIndex(x => new { x.LectureSeriesId, x.EpisodeNumber });

        b.HasOne(x => x.Speaker).WithMany(s => s.Lectures)
         .HasForeignKey(x => x.SpeakerId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.LectureSeries).WithMany(s => s.Lectures)
         .HasForeignKey(x => x.LectureSeriesId).OnDelete(DeleteBehavior.SetNull);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}