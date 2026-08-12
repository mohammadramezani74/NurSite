using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NurSite.Application.Interfaces;
using NurSite.Domain.Entities;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Areas.Admin.Pages.TaghvimGhamari;

/// <summary>
/// ثبت آغاز ماه‌های قمری از تقویم رسمی ایران.
/// سالی یک بار، دوازده ردیف — و تاریخ همه مناسبت‌ها دقیق می‌شود.
/// </summary>
public class IndexModel(
    AppDbContext db,
    IMemoryCache cache,
    IPersianDateService dates) : PageModel
{
    private static readonly UmAlQuraCalendar Hijri = new();
    private static readonly PersianCalendar Persian = new();

    public static readonly string[] HijriMonths =
    {
        "محرم","صفر","ربیع‌الأول","ربیع‌الثانی","جمادی‌الأول","جمادی‌الثانی",
        "رجب","شعبان","رمضان","شوال","ذی‌القعده","ذی‌الحجه"
    };

    /// <summary>یک ماه قمری با مقدار ثبت‌شده و مقدار پیشنهادی محاسبه.</summary>
    public sealed record MonthRow(
        int Month,
        string MonthName,
        DateOnly? SavedStart,
        DateOnly? ComputedStart,
        string? Note,
        int? DayCount);

    public IReadOnlyList<MonthRow> Rows { get; private set; } = [];
    public int HijriYear { get; private set; }
    public int SavedCount { get; private set; }

    [BindProperty] public InputModel Input { get; set; } = new();

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class InputModel
    {
        [Range(1300, 1600)]
        public int HijriYear { get; set; }

        /// <summary>تاریخ شمسی آغاز هر ماه، به شکل ۱۴۰۵/۰۵/۳۰ — خالی یعنی ثبت نشده.</summary>
        public string?[] Starts { get; set; } = new string?[12];

        public string?[] Notes { get; set; } = new string?[12];
    }

    public async Task OnGetAsync(int? sal, CancellationToken ct)
    {
        HijriYear = sal ?? Hijri.GetYear(DateTime.UtcNow);
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        HijriYear = Input.HijriYear;

        var existing = await db.HijriMonthStarts
            .Where(m => m.HijriYear == HijriYear)
            .ToListAsync(ct);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var errors = new List<string>();

        for (var i = 0; i < 12; i++)
        {
            var month = i + 1;
            var raw = Input.Starts.ElementAtOrDefault(i);
            var note = Input.Notes.ElementAtOrDefault(i)?.Trim();
            var row = existing.FirstOrDefault(m => m.HijriMonth == month);

            // خالی یعنی این ماه ثبت نشود یا ثبت قبلی حذف شود
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (row is not null) db.HijriMonthStarts.Remove(row);
                continue;
            }

            if (!TryParsePersianDate(raw, out var date))
            {
                errors.Add($"تاریخ ماه {HijriMonths[i]} خوانده نشد: «{raw}»");
                continue;
            }

            if (row is null)
            {
                db.HijriMonthStarts.Add(new HijriMonthStart
                {
                    HijriYear = HijriYear,
                    HijriMonth = month,
                    StartsOn = date,
                    Note = note,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedById = userId
                });
            }
            else
            {
                row.StartsOn = date;
                row.Note = note;
            }
        }

        // بررسی منطقی: هر ماه باید بعد از ماه قبل باشد و ۲۹ یا ۳۰ روز فاصله داشته باشد
        await db.SaveChangesAsync(ct);
        errors.AddRange(await ValidateSequenceAsync(ct));

        ClearCaches();

        Flash = errors.Count == 0
            ? "تقویم قمری ذخیره شد."
            : "ذخیره شد، اما: " + string.Join(" · ", errors);
        FlashKind = errors.Count == 0 ? "ok" : "warn";

        return RedirectToPage(new { sal = HijriYear });
    }

    /// <summary>پر کردن خودکار همه ماه‌ها با مقادیر محاسبه‌شده، برای شروع کار.</summary>
    public async Task<IActionResult> OnPostFillAsync(int sal, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var existing = await db.HijriMonthStarts
            .Where(m => m.HijriYear == sal)
            .Select(m => m.HijriMonth)
            .ToListAsync(ct);

        var added = 0;
        for (var month = 1; month <= 12; month++)
        {
            if (existing.Contains(month)) continue;
            if (!TryComputed(sal, month, out var date)) continue;

            db.HijriMonthStarts.Add(new HijriMonthStart
            {
                HijriYear = sal,
                HijriMonth = month,
                StartsOn = date,
                Note = "پیش‌فرض محاسبه‌شده — با تقویم رسمی تطبیق داده شود",
                CreatedAtUtc = DateTime.UtcNow,
                CreatedById = userId
            });
            added++;
        }

        await db.SaveChangesAsync(ct);
        ClearCaches();

        Flash = $"{dates.ToPersianDigits(added.ToString())} ماه با مقدار محاسبه‌شده پر شد. حتماً با تقویم رسمی تطبیق دهید.";
        FlashKind = "warn";
        return RedirectToPage(new { sal });
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var saved = await db.HijriMonthStarts.AsNoTracking()
            .Where(m => m.HijriYear == HijriYear)
            .ToDictionaryAsync(m => m.HijriMonth, m => m, ct);

        // برای محاسبه تعداد روزهای هر ماه، آغاز ماه اول سال بعد هم لازم است
        var nextYearFirst = await db.HijriMonthStarts.AsNoTracking()
            .Where(m => m.HijriYear == HijriYear + 1 && m.HijriMonth == 1)
            .Select(m => (DateOnly?)m.StartsOn)
            .FirstOrDefaultAsync(ct);

        var rows = new List<MonthRow>(12);
        for (var month = 1; month <= 12; month++)
        {
            saved.TryGetValue(month, out var row);
            DateOnly? computed = TryComputed(HijriYear, month, out var c) ? c : null;

            // تعداد روزهای ماه از فاصله تا ماه بعد به دست می‌آید
            int? dayCount = null;
            if (row is not null)
            {
                DateOnly? next = month == 12
                    ? nextYearFirst
                    : saved.TryGetValue(month + 1, out var n) ? n.StartsOn : null;

                if (next is not null)
                    dayCount = next.Value.DayNumber - row.StartsOn.DayNumber;
            }

            rows.Add(new MonthRow(month, HijriMonths[month - 1],
                row?.StartsOn, computed, row?.Note, dayCount));
        }

        Rows = rows;
        SavedCount = saved.Count;
        Input.HijriYear = HijriYear;
    }

    private async Task<List<string>> ValidateSequenceAsync(CancellationToken ct)
    {
        var errors = new List<string>();

        var all = await db.HijriMonthStarts.AsNoTracking()
            .Where(m => m.HijriYear == HijriYear)
            .OrderBy(m => m.HijriMonth)
            .ToListAsync(ct);

        for (var i = 1; i < all.Count; i++)
        {
            if (all[i].HijriMonth != all[i - 1].HijriMonth + 1) continue;

            var gap = all[i].StartsOn.DayNumber - all[i - 1].StartsOn.DayNumber;

            // ماه قمری همیشه ۲۹ یا ۳۰ روز است
            if (gap is < 29 or > 30)
            {
                errors.Add(
                    $"فاصله {HijriMonths[all[i - 1].HijriMonth - 1]} تا " +
                    $"{HijriMonths[all[i].HijriMonth - 1]} برابر {gap} روز است، باید ۲۹ یا ۳۰ باشد");
            }
        }

        return errors;
    }

    private void ClearCaches()
    {
        // نتایج مناسبت‌ها بر اساس بازه تاریخ کش می‌شوند و کلیدشان متغیر است.
        // ساده‌ترین راه مطمئن، پاک کردن کلیدهای شناخته‌شده است.
        cache.Remove("site:settings");
        cache.Remove($"theme:occasion:{DateTime.UtcNow:yyyy-MM-dd}");

        if (cache is MemoryCache mc) mc.Clear();
    }

    /// <summary>تاریخ شمسی به شکل ۱۴۰۵/۰۵/۳۰ یا ۱۴۰۵-۵-۳۰ را می‌خواند.</summary>
    private static bool TryParsePersianDate(string input, out DateOnly date)
    {
        date = default;

        var normalized = new string(input
            .Select(c => c is >= '\u06F0' and <= '\u06F9' ? (char)(c - '\u06F0' + '0')
                       : c is >= '\u0660' and <= '\u0669' ? (char)(c - '\u0660' + '0')
                       : c)
            .ToArray());

        var parts = normalized.Split('/', '-', '.', ' ')
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        if (parts.Length != 3) return false;
        if (!int.TryParse(parts[0], out var y) ||
            !int.TryParse(parts[1], out var m) ||
            !int.TryParse(parts[2], out var d)) return false;

        if (y is < 1300 or > 1500 || m is < 1 or > 12 || d is < 1 or > 31) return false;

        try
        {
            date = DateOnly.FromDateTime(Persian.ToDateTime(y, m, d, 0, 0, 0, 0));
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool TryComputed(int hijriYear, int month, out DateOnly date)
    {
        date = default;
        try
        {
            date = DateOnly.FromDateTime(Hijri.ToDateTime(hijriYear, month, 1, 0, 0, 0, 0));
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    public string ToPersianDateString(DateOnly? date)
    {
        if (date is null) return "";
        var dt = date.Value.ToDateTime(TimeOnly.MinValue);
        return $"{Persian.GetYear(dt)}/{Persian.GetMonth(dt):00}/{Persian.GetDayOfMonth(dt):00}";
    }

    public string ToPersianDisplay(DateOnly? date)
    {
        if (date is null) return "—";
        return dates.ToPersianDate(date.Value.ToDateTime(TimeOnly.MinValue), includeWeekday: true);
    }
}