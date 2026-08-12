using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Common;
using NurSite.Domain.Entities;
using NurSite.Infrastructure.Identity;

namespace NurSite.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();

    public DbSet<Ruling> Rulings => Set<Ruling>();
    public DbSet<RulingCategory> RulingCategories => Set<RulingCategory>();
    public DbSet<Marja> Marjas => Set<Marja>();
    public DbSet<UserQuestion> UserQuestions => Set<UserQuestion>();

    public DbSet<Lecture> Lectures => Set<Lecture>();
    public DbSet<LectureSeries> LectureSeries => Set<LectureSeries>();
    public DbSet<Speaker> Speakers => Set<Speaker>();

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Occasion> Occasions => Set<Occasion>();
    public DbSet<HijriMonthStart> HijriMonthStarts => Set<HijriMonthStart>();

    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Photo> Photos => Set<Photo>();

    public DbSet<City> Cities => Set<City>();
    public DbSet<HeroVerse> HeroVerses => Set<HeroVerse>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Subscriber> Subscribers => Set<Subscriber>();
    public DbSet<UrlRedirect> UrlRedirects => Set<UrlRedirect>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // نام‌گذاری یکدست برای همه جدول‌های Identity
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");

        // ستون‌های شناسه کاربر باید طول داشته باشند تا قابل ایندکس شوند.
        // بدون این، nvarchar(max) می‌شوند و SQL Server اجازه ایندکس نمی‌دهد.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(IAuditable).IsAssignableFrom(entityType.ClrType)) continue;

            entityType.GetProperty(nameof(IAuditable.CreatedById)).SetMaxLength(450);
            entityType.GetProperty(nameof(IAuditable.UpdatedById)).SetMaxLength(450);
        }

        // تطبیق حروف فارسی و عربی هنگام مقایسه و جستجو.
        // اگر سرور شما این collation را نداشت، این خط را کامنت کنید —
        // با collation پیش‌فرض هم کار می‌کند، فقط جستجو به اعراب حساس‌تر می‌شود.
        builder.UseCollation("Persian_100_CI_AI");
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(ct);
    }

    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return base.SaveChanges();
    }

    /// <summary>پر کردن خودکار فیلدهای تاریخ ایجاد و ویرایش.</summary>
    private void ApplyAuditInfo()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAtUtc = now;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAtUtc = now;
        }

        // حذف نرم به‌جای حذف فیزیکی
        foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State != EntityState.Deleted) continue;
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAtUtc = now;
        }
    }
}