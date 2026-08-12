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
/// اولویت با جدول آغاز ماه‌های قمری است که ادمین از تقویم رسمی ایران وارد
/// می‌کند. اگر ماهی در آن جدول نباشد، به محاسبه ام‌القری برمی‌گردیم که
/// تقریبی است — اختلافش با تقویم ایران ماه به ماه فرق می‌کند و با یک عدد
/// ثابت قابل جبران نیست.
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

            // جدول ماه‌های ثبت‌شده، برای جستجوی سریع
            var monthStarts = await db.HijriMonthStarts.AsNoTracking()
                .ToDictionaryAsync(m => (m.HijriYear, m.HijriMonth), m => m.StartsOn, ct);

            var result = new List<OccasionOccurrence>();

            // بازه کمی بازتر تا مناسبت‌های لبه از قلم نیفتند
            var startYear = SafeHijriYear(from.AddDays(-40));
            var endYear = SafeHijriYear(to.AddDays(40));

            for (var year = startYear; year <= endYear + 1; year++)
            {
                foreach (var o in occasions)
                {
                    if (!TryResolveDate(year, o.HijriMonth, o.HijriDay, monthStarts, out var date))
                        continue;

                    if (date < from || date > to) continue;

                    result.Add(new OccasionOccurrence(
                        o.Id, o.Title, o.Slug, o.Description, o.Kind,
                        o.IsPublicHoliday, date, o.HijriMonth, o.HijriDay));
                }
            }

            return result
                .DistinctBy(x => (x.OccasionId, x.Date))
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Title)
                .ToList();
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

    public async Task<HijriDate> ToHijriAsync(DateOnly date, CancellationToken ct = default)
    {
        var monthStarts = await cache.GetOrCreateAsync("hijri:month-starts", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
            return await db.HijriMonthStarts.AsNoTracking()
                .OrderBy(m => m.StartsOn)
                .Select(m => new { m.HijriYear, m.HijriMonth, m.StartsOn })
                .ToListAsync(ct);
        });

        // آخرین ماهی که آغازش از این تاریخ جلوتر نیست
        var match = monthStarts?
            .Where(m => m.StartsOn <= date)
            .OrderByDescending(m => m.StartsOn)
            .FirstOrDefault();

        if (match is not null)
        {
            var day = date.DayNumber - match.StartsOn.DayNumber + 1;

            // اگر روز از ۳۰ گذشت یعنی ماه بعد ثبت نشده و داریم از مرز بیرون می‌زنیم
            if (day is >= 1 and <= 30)
                return new HijriDate(match.HijriYear, match.HijriMonth, day, true);
        }

        // برگشت به محاسبه تقریبی
        var dt = date.ToDateTime(TimeOnly.MinValue);
        try
        {
            return new HijriDate(Hijri.GetYear(dt), Hijri.GetMonth(dt), Hijri.GetDayOfMonth(dt), false);
        }
        catch (ArgumentOutOfRangeException)
        {
            return new HijriDate(1448, 1, 1, false);
        }
    }

    /// <summary>
    /// تاریخ میلادی یک روز قمری. اول از جدول رسمی، بعد از محاسبه ام‌القری.
    /// </summary>
    private static bool TryResolveDate(
        int hijriYear, int month, int day,
        IReadOnlyDictionary<(int, int), DateOnly> monthStarts,
        out DateOnly date)
    {
        // مسیر دقیق: آغاز ماه از تقویم رسمی ثبت شده است
        if (monthStarts.TryGetValue((hijriYear, month), out var start))
        {
            date = start.AddDays(day - 1);

            // اگر روز از ماه بیرون زد — مثلاً روز ۳۰ در ماهی که ۲۹ روزه است —
            // و ماه بعد هم ثبت شده باشد، این تاریخ نامعتبر است
            if (monthStarts.TryGetValue(NextMonthKey(hijriYear, month), out var nextStart)
                && date >= nextStart)
            {
                date = default;
                return false;
            }

            return true;
        }

        // مسیر تقریبی
        return TryToGregorian(hijriYear, month, day, out date);
    }

    private static (int, int) NextMonthKey(int year, int month) =>
        month == 12 ? (year + 1, 1) : (year, month + 1);

    private static int SafeHijriYear(DateOnly date)
    {
        try
        {
            return Hijri.GetYear(date.ToDateTime(TimeOnly.MinValue));
        }
        catch (ArgumentOutOfRangeException)
        {
            return Hijri.GetYear(DateTime.UtcNow);
        }
    }

    private static bool TryToGregorian(int hijriYear, int month, int day, out DateOnly date)
    {
        date = default;
        try
        {
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