using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Identity;

namespace NurSite.Infrastructure.Persistence.Seed;

/// <summary>مقداردهی اولیه دیتابیس: نقش‌ها، دسترسی‌ها، مدیر ارشد و داده‌های پایه.</summary>
public static class DbInitializer
{
    public static async Task SeedAsync(
        AppDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        ILogger logger,
        string adminMobile,
        string adminPassword,
        CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        await SeedRolesAsync(roles, logger);
        await SeedSuperAdminAsync(users, logger, adminMobile, adminPassword);
        await SeedCitiesAsync(db, ct);
        await SeedHeroVersesAsync(db, ct);
        await SeedOccasionsAsync(db, ct);
        await SeedSettingsAsync(db, ct);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("مقداردهی اولیه دیتابیس کامل شد.");
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roles, ILogger logger)
    {
        foreach (var (name, display, description) in AppRoles.All)
        {
            var role = await roles.FindByNameAsync(name);
            if (role is null)
            {
                role = new ApplicationRole(name) { DisplayName = display, Description = description };
                var result = await roles.CreateAsync(role);
                if (!result.Succeeded)
                {
                    logger.LogError("ساخت نقش {Role} ناموفق بود: {Errors}",
                        name, string.Join(" | ", result.Errors.Select(e => e.Description)));
                    continue;
                }
            }

            // دسترسی‌های پیش‌فرض به صورت Claim
            if (!Permissions.DefaultsByRole.TryGetValue(name, out var perms)) continue;
            var existing = await roles.GetClaimsAsync(role);
            foreach (var p in perms)
            {
                if (existing.Any(c => c.Type == Permissions.ClaimType && c.Value == p)) continue;
                await roles.AddClaimAsync(role, new System.Security.Claims.Claim(Permissions.ClaimType, p));
            }
        }
    }

    private static async Task SeedSuperAdminAsync(
        UserManager<ApplicationUser> users, ILogger logger, string mobile, string password)
    {
        if (string.IsNullOrWhiteSpace(mobile) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("اطلاعات مدیر ارشد تنظیم نشده است؛ از ساخت آن صرف‌نظر شد.");
            return;
        }

        var user = await users.FindByNameAsync(mobile);
        if (user is not null) return;

        user = new ApplicationUser
        {
            UserName = mobile,
            PhoneNumber = mobile,
            PhoneNumberConfirmed = true,
            FullName = "مدیر ارشد",
            EmailConfirmed = true
        };

        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogError("ساخت مدیر ارشد ناموفق بود: {Errors}",
                string.Join(" | ", result.Errors.Select(e => e.Description)));
            return;
        }
        await users.AddToRoleAsync(user, AppRoles.SuperAdmin);
        logger.LogInformation("مدیر ارشد با شماره {Mobile} ساخته شد.", mobile);
    }

    private static async Task SeedCitiesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Cities.AnyAsync(ct)) return;

        db.Cities.AddRange(
            new City { Name = "تهران", Slug = "tehran", ProvinceName = "تهران", Latitude = 35.6892, Longitude = 51.3890, Elevation = 1200, IsDefault = true, SortOrder = 1 },
            new City { Name = "قم", Slug = "qom", ProvinceName = "قم", Latitude = 34.6416, Longitude = 50.8746, Elevation = 928, SortOrder = 2 },
            new City { Name = "مشهد", Slug = "mashhad", ProvinceName = "خراسان رضوی", Latitude = 36.2605, Longitude = 59.6168, Elevation = 995, SortOrder = 3 },
            new City { Name = "اصفهان", Slug = "isfahan", ProvinceName = "اصفهان", Latitude = 32.6539, Longitude = 51.6660, Elevation = 1590, SortOrder = 4 },
            new City { Name = "شیراز", Slug = "shiraz", ProvinceName = "فارس", Latitude = 29.5918, Longitude = 52.5837, Elevation = 1500, SortOrder = 5 },
            new City { Name = "تبریز", Slug = "tabriz", ProvinceName = "آذربایجان شرقی", Latitude = 38.0800, Longitude = 46.2919, Elevation = 1350, SortOrder = 6 }
        );
    }

    private static async Task SeedHeroVersesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.HeroVerses.AnyAsync(ct)) return;

        db.HeroVerses.AddRange(
            new HeroVerse { ArabicText = "أَلَا بِذِكْرِ اللَّهِ تَطْمَئِنُّ الْقُلُوبُ", PersianText = "آگاه باشید که دل‌ها تنها با یاد خدا آرام می‌گیرد", Reference = "سوره رعد · آیه ۲۸", SortOrder = 1 },
            new HeroVerse { ArabicText = "وَذَكِّرْ فَإِنَّ الذِّكْرَىٰ تَنفَعُ الْمُؤْمِنِينَ", PersianText = "و یادآوری کن، که یادآوری مؤمنان را سود می‌رساند", Reference = "سوره ذاریات · آیه ۵۵", SortOrder = 2 },
            new HeroVerse { ArabicText = "وَمَن يَتَّقِ اللَّهَ يَجْعَل لَّهُ مَخْرَجًا", PersianText = "و هر کس پروای خدا داشته باشد، خدا راهی برای او می‌گشاید", Reference = "سوره طلاق · آیه ۲", SortOrder = 3 },
            new HeroVerse { ArabicText = "إِنَّ اللَّهَ مَعَ الصَّابِرِينَ", PersianText = "همانا خداوند با شکیبایان است", Reference = "سوره بقره · آیه ۱۵۳", SortOrder = 4 },
            new HeroVerse { ArabicText = "إِنَّمَا يُرِيدُ اللَّهُ لِيُذْهِبَ عَنكُمُ الرِّجْسَ أَهْلَ الْبَيْتِ", PersianText = "خداوند تنها می‌خواهد پلیدی را از شما اهل بیت دور کند", Reference = "سوره احزاب · آیه ۳۳", SortOrder = 5 }
        );
    }

    private static async Task SeedOccasionsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Occasions.AnyAsync(ct)) return;

        // مناسبت‌هایی که پوسته سایت را خودکار عوض می‌کنند
        db.Occasions.AddRange(
            new Occasion { Title = "تاسوعای حسینی", Slug = "tasua", HijriMonth = 1, HijriDay = 9, Kind = OccasionKind.Mourning, ForcedTheme = SiteTheme.Anabi, ThemeStartsDaysBefore = 9, ThemeEndsDaysAfter = 3 },
            new Occasion { Title = "عاشورای حسینی", Slug = "ashura", HijriMonth = 1, HijriDay = 10, Kind = OccasionKind.Mourning, IsPublicHoliday = true },
            new Occasion { Title = "اربعین حسینی", Slug = "arbaeen", HijriMonth = 2, HijriDay = 20, Kind = OccasionKind.Mourning, IsPublicHoliday = true, ForcedTheme = SiteTheme.Anabi, ThemeStartsDaysBefore = 5, ThemeEndsDaysAfter = 2 },
            new Occasion { Title = "رحلت پیامبر اکرم ﷺ", Slug = "rehlat-payambar", HijriMonth = 2, HijriDay = 28, Kind = OccasionKind.Mourning, IsPublicHoliday = true, ForcedTheme = SiteTheme.Anabi, ThemeStartsDaysBefore = 2, ThemeEndsDaysAfter = 1 },
            new Occasion { Title = "شهادت امام حسن عسکری ؑ", Slug = "shahadat-emam-askari", HijriMonth = 3, HijriDay = 8, Kind = OccasionKind.Mourning, IsPublicHoliday = true },
            // آغاز امامت همان روز شهادت امام حسن عسکری است، نه فردایش
            new Occasion { Title = "آغاز امامت حضرت ولی‌عصر ؑ", Slug = "aghaz-emamat", HijriMonth = 3, HijriDay = 8, Kind = OccasionKind.Celebration, ForcedTheme = SiteTheme.Sabz, ThemeStartsDaysBefore = 0, ThemeEndsDaysAfter = 1 },
            new Occasion { Title = "ولادت پیامبر اکرم ﷺ", Slug = "veladat-payambar", HijriMonth = 3, HijriDay = 17, Kind = OccasionKind.Celebration, IsPublicHoliday = true, ForcedTheme = SiteTheme.Sabz, ThemeStartsDaysBefore = 1, ThemeEndsDaysAfter = 1 },
            new Occasion { Title = "عید سعید فطر", Slug = "eid-fetr", HijriMonth = 10, HijriDay = 1, Kind = OccasionKind.Celebration, IsPublicHoliday = true, ForcedTheme = SiteTheme.Sabz, ThemeStartsDaysBefore = 0, ThemeEndsDaysAfter = 2 },
            new Occasion { Title = "عید سعید غدیر خم", Slug = "eid-ghadir", HijriMonth = 12, HijriDay = 18, Kind = OccasionKind.Celebration, IsPublicHoliday = true, ForcedTheme = SiteTheme.Sabz, ThemeStartsDaysBefore = 1, ThemeEndsDaysAfter = 1 }
        );
    }

    private static async Task SeedSettingsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.SiteSettings.AnyAsync(ct)) return;

        db.SiteSettings.Add(new SiteSetting
        {
            SiteName = "مؤسسه فرهنگی نورالثقلین",
            Tagline = "خانه‌ای برای آموختن، اندیشیدن و با هم بودن",
            DefaultMetaTitle = "مؤسسه فرهنگی نورالثقلین",
            DefaultMetaDescription = "اوقات شرعی، برنامه‌های هفتگی، احکام، مقالات و آرشیو سخنرانی‌های صوتی.",
            CanonicalBaseUrl = "https://example.ir",
            DefaultTheme = SiteTheme.Lajvard,
            AllowUserThemeChoice = true,
            EnableOccasionTheme = true,
            WorkingHours = "شنبه تا چهارشنبه، ۹ تا ۱۹"
        });
    }
}