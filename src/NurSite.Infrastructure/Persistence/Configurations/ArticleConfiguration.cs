using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> b)
    {
        b.Property(x => x.Title).HasMaxLength(250).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(250).IsRequired();
        b.Property(x => x.Summary).HasMaxLength(500);
        b.Property(x => x.MetaTitle).HasMaxLength(70);
        b.Property(x => x.MetaDescription).HasMaxLength(170);
        b.Property(x => x.CoverImagePath).HasMaxLength(400);
        b.Property(x => x.CoverImageAlt).HasMaxLength(250);
        b.Property(x => x.AuthorDisplayName).HasMaxLength(150);
        b.Property(x => x.AuthorId).HasMaxLength(450);
        b.Property(x => x.OgImagePath).HasMaxLength(400);

        // اسلاگ باید یکتا باشد ولی ردیف‌های حذف‌شده نباید مانع شوند
        b.HasIndex(x => x.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasIndex(x => new { x.Status, x.PublishedAtUtc });
        b.HasIndex(x => x.CategoryId);
        // برای صفحه «مقالات من» در پنل نویسنده
        b.HasIndex(x => new { x.AuthorId, x.Status });

        b.HasOne(x => x.Category)
         .WithMany(c => c.Articles)
         .HasForeignKey(x => x.CategoryId)
         .OnDelete(DeleteBehavior.Restrict);

        // فیلتر سراسری: محتوای حذف‌شده هرگز در کوئری‌های عادی نمی‌آید
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
