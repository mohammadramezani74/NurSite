using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;

namespace NurSite.Infrastructure.Persistence.Configurations;

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

        // ستون جستجو متن بلندی است و ایندکس نمی‌گیرد؛ جستجو با LIKE
        // انجام می‌شود که برای حجم این سایت کافی است
        b.Property(x => x.SearchText).HasColumnType("nvarchar(max)");

        // فیلتر سراسری: محتوای حذف‌شده هرگز در کوئری‌های عادی نمی‌آید
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.Property(x => x.Title).HasMaxLength(150).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.MetaTitle).HasMaxLength(70);
        b.Property(x => x.MetaDescription).HasMaxLength(170);
        b.Property(x => x.OgImagePath).HasMaxLength(400);
        b.HasIndex(x => x.Slug).IsUnique();

        b.HasOne(x => x.Parent)
         .WithMany(x => x.Children)
         .HasForeignKey(x => x.ParentId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> b)
    {
        b.Property(x => x.Title).HasMaxLength(80).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(80).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
    }
}

public class ArticleTagConfiguration : IEntityTypeConfiguration<ArticleTag>
{
    public void Configure(EntityTypeBuilder<ArticleTag> b)
    {
        b.HasKey(x => new { x.ArticleId, x.TagId });
        b.HasOne(x => x.Article).WithMany(a => a.ArticleTags).HasForeignKey(x => x.ArticleId);
        b.HasOne(x => x.Tag).WithMany(t => t.ArticleTags).HasForeignKey(x => x.TagId);
    }
}

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

        b.Property(x => x.SearchText).HasColumnType("nvarchar(max)");

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class RulingCategoryConfiguration : IEntityTypeConfiguration<RulingCategory>
{
    public void Configure(EntityTypeBuilder<RulingCategory> b)
    {
        b.Property(x => x.Title).HasMaxLength(150).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(150).IsRequired();
        b.Property(x => x.IconName).HasMaxLength(60);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.MetaTitle).HasMaxLength(70);
        b.Property(x => x.MetaDescription).HasMaxLength(170);
        b.Property(x => x.OgImagePath).HasMaxLength(400);
        b.HasIndex(x => x.Slug).IsUnique();
    }
}

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

public class UserQuestionConfiguration : IEntityTypeConfiguration<UserQuestion>
{
    public void Configure(EntityTypeBuilder<UserQuestion> b)
    {
        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        b.Property(x => x.SenderName).HasMaxLength(150);
        b.Property(x => x.SenderMobile).HasMaxLength(15);
        b.Property(x => x.SenderEmail).HasMaxLength(200);
        b.Property(x => x.AssignedToUserId).HasMaxLength(450);
        b.Property(x => x.TrackingCode).HasMaxLength(12).IsRequired();
        b.Property(x => x.SenderIpHash).HasMaxLength(64);

        // پیگیری با کد رهگیری انجام می‌شود، پس باید یکتا و ایندکس‌دار باشد
        b.HasIndex(x => x.TrackingCode).IsUnique();
        b.HasIndex(x => x.Status);
        // صف پرسش‌های ارجاع‌شده به هر پاسخگو
        b.HasIndex(x => new { x.AssignedToUserId, x.Status });

        b.HasOne(x => x.PublishedRuling)
         .WithMany()
         .HasForeignKey(x => x.PublishedRulingId)
         .OnDelete(DeleteBehavior.SetNull);
    }
}