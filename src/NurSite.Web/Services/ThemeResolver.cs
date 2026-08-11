using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Services;

public sealed record ResolvedTheme(string Name, bool IsForced, string? Reason);

/// <summary>
/// تعیین پوسته فعال. ترتیب اولویت:
/// ۱) انتخاب صریح کاربر در کوکی — همیشه مقدم است
/// ۲) مناسبت عزا یا عید که پوسته پیشنهادی دارد
/// ۳) پوسته پیش‌فرض تنظیمات سایت
/// </summary>
public sealed class ThemeResolver(AppDbContext db, IMemoryCache cache)
{
    public const string CookieName = "nur.theme";

    public async Task<ResolvedTheme> ResolveAsync(string? cookieValue, CancellationToken ct = default)
    {
        // اگر دیتابیس در دسترس نباشد، Layout نباید بشکند — وگرنه صفحه خطا
        // که خودش از همین Layout استفاده می‌کند هم از کار می‌افتد و کاربر
        // به‌جای پیام خطا، صفحه سفید می‌بیند.
        try
        {
            return await ResolveCoreAsync(cookieValue, ct);
        }
        catch
        {
            return new ResolvedTheme(SiteTheme.Lajvard.ToString().ToLowerInvariant(), false, null);
        }
    }

    private async Task<ResolvedTheme> ResolveCoreAsync(string? cookieValue, CancellationToken ct)
    {
        var settings = await cache.GetOrCreateAsync("site:settings", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        });

        var fallback = (settings?.DefaultTheme ?? SiteTheme.Lajvard).ToString().ToLowerInvariant();

        // ۱) انتخاب صریح کاربر — بر همه چیز مقدم است
        if (settings?.AllowUserThemeChoice == true &&
            !string.IsNullOrWhiteSpace(cookieValue) &&
            Enum.TryParse<SiteTheme>(cookieValue, ignoreCase: true, out var chosen))
        {
            return new ResolvedTheme(chosen.ToString().ToLowerInvariant(), false, null);
        }

        // ۲) مناسبت — فقط وقتی کاربر خودش چیزی انتخاب نکرده
        if (settings?.EnableOccasionTheme == true)
        {
            var occasion = await GetActiveOccasionThemeAsync(ct);
            if (occasion is not null)
                return new ResolvedTheme(occasion.Value.Theme, true, occasion.Value.Title);
        }

        // ۳) پیش‌فرض
        return new ResolvedTheme(fallback, false, null);
    }

    private async Task<(string Theme, string Title)?> GetActiveOccasionThemeAsync(CancellationToken ct)
    {
        var todayKey = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return await cache.GetOrCreateAsync($"theme:occasion:{todayKey}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);

            var candidates = await db.Occasions.AsNoTracking()
                .Where(o => o.IsActive && o.ForcedTheme != null)
                .ToListAsync(ct);

            var hijri = new UmAlQuraCalendar();
            var now = DateTime.UtcNow;

            foreach (var o in candidates)
            {
                // تاریخ میلادی مناسبت در سال قمری جاری
                var currentHijriYear = hijri.GetYear(now);
                DateTime occasionDate;
                try
                {
                    occasionDate = hijri.ToDateTime(currentHijriYear, o.HijriMonth, o.HijriDay, 0, 0, 0, 0);
                }
                catch (ArgumentOutOfRangeException)
                {
                    continue; // مثلاً روز ۳۰ در ماهی که ۲۹ روز دارد
                }

                var from = occasionDate.AddDays(-o.ThemeStartsDaysBefore);
                var to = occasionDate.AddDays(o.ThemeEndsDaysAfter + 1);

                if (now >= from && now < to)
                    return (o.ForcedTheme!.Value.ToString().ToLowerInvariant(), o.Title);
            }
            return ((string, string)?)null;
        });
    }
}