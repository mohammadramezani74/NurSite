using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NurSite.Application.DTOs;
using NurSite.Application.Interfaces;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Infrastructure.Services;

/// <summary>
/// تبدیل مناسبت‌های قمری به تاریخ میلادی.
///
/// هر مناسبت با روز و ماه قمری ذخیره شده و هر سال تکرار می‌شود، پس برای
/// یافتن اینکه در یک بازه میلادی کجا می‌افتد، باید سال‌های قمری مربوطه را
/// امتحان کرد.
/// </summary>
public sealed class OccasionService(AppDbContext db, IMemoryCache cache) : IOccasionService
{
    private static readonly UmAlQuraCalendar Hijri = new();

    public async Task<IReadOnlyList<OccasionOccurrence>> GetInRangeAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from) (from, to) = (to, from);

        var key = $"occasions:{from:yyyy-MM-dd}:{to:yyyy-MM-dd}";

        var cached = await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

            var occasions = await db.Occasions.AsNoTracking()
                .Where(o => o.IsActive)
                .ToListAsync(ct);

            // اختلاف تقویم ام‌القری با تقویم قمری ایران
            var siteSetting = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
            var offset = siteSetting?.HijriDayOffset ?? 1;

            var result = new List<OccasionOccurrence>();

            // سال قمری ابتدا و انتهای بازه ممکن است متفاوت باشند
            // بازه سال قمری کمی بازتر گرفته می‌شود تا مناسبت‌هایی که با
            // اعمال آفست به داخل بازه می‌آیند از قلم نیفتند
            var startYear = SafeHijriYear(from.AddDays(-5));
            var endYear = SafeHijriYear(to.AddDays(5));

            for (var year = startYear; year <= endYear + 1; year++)
            {
                foreach (var o in occasions)
                {
                    if (!TryToGregorian(year, o.HijriMonth, o.HijriDay, out var date)) continue;

                    date = date.AddDays(offset);
                    if (date < from || date > to) continue;

                    result.Add(new OccasionOccurrence(
                        o.Id, o.Title, o.Slug, o.Description, o.Kind,
                        o.IsPublicHoliday, date, o.HijriMonth, o.HijriDay));
                }
            }

            return result.OrderBy(x => x.Date).ThenBy(x => x.Title).ToList();
        });

        return cached ?? [];
    }

    public async Task<IReadOnlyList<OccasionOccurrence>> GetUpcomingAsync(
        int take = 6, int withinDays = 120, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3.5));
        var all = await GetInRangeAsync(today, today.AddDays(withinDays), ct);
        return all.Take(take).ToList();
    }

    private static int SafeHijriYear(DateOnly date)
    {
        try
        {
            return Hijri.GetYear(date.ToDateTime(TimeOnly.MinValue));
        }
        catch (ArgumentOutOfRangeException)
        {
            // تقویم ام‌القری بازه محدودی دارد؛ خارج از آن به سال جاری برمی‌گردیم
            return Hijri.GetYear(DateTime.UtcNow);
        }
    }

    private static bool TryToGregorian(int hijriYear, int month, int day, out DateOnly date)
    {
        date = default;
        try
        {
            // روز ۳۰ در ماهی که ۲۹ روز دارد، استثنا می‌دهد
            var dt = Hijri.ToDateTime(hijriYear, month, day, 0, 0, 0, 0);
            date = DateOnly.FromDateTime(dt);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}