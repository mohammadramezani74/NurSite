using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.DTOs;
using NurSite.Application.Interfaces;
using NurSite.Domain.Entities;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Pages;

public class OwqatModel(
    AppDbContext db,
    IPrayerTimeService prayerTimes,
    IOccasionService occasions,
    IPersianDateService dates) : PageModel
{
    private static readonly PersianCalendar Persian = new();

    public IReadOnlyList<City> Cities { get; private set; } = [];
    public City? SelectedCity { get; private set; }

    public PrayerTimesDto? Today { get; private set; }
    public string? NextPrayerName { get; private set; }
    public string? TimeUntilNext { get; private set; }

    /// <summary>اوقات همه روزهای ماه شمسی جاری یا انتخاب‌شده.</summary>
    public IReadOnlyList<PrayerTimesDto> MonthDays { get; private set; } = [];

    /// <summary>مناسبت‌های همان ماه، برای نمایش در کنار روزها.</summary>
    public IReadOnlyDictionary<DateOnly, List<OccasionOccurrence>> MonthOccasions { get; private set; }
        = new Dictionary<DateOnly, List<OccasionOccurrence>>();

    public string MonthTitle { get; private set; } = "";
    public int PersianYear { get; private set; }
    public int PersianMonth { get; private set; }
    public DateOnly TodayLocal { get; private set; }

    [BindProperty(SupportsGet = true, Name = "shahr")] public string? CitySlug { get; set; }
    [BindProperty(SupportsGet = true, Name = "sal")] public int? Year { get; set; }
    [BindProperty(SupportsGet = true, Name = "mah")] public int? Month { get; set; }

    private static readonly string[] PersianMonths =
    {
        "فروردین","اردیبهشت","خرداد","تیر","مرداد","شهریور",
        "مهر","آبان","آذر","دی","بهمن","اسفند"
    };

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        Cities = await db.Cities.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync(ct);

        if (Cities.Count == 0) return NotFound();

        SelectedCity = (!string.IsNullOrWhiteSpace(CitySlug)
                           ? Cities.FirstOrDefault(c => c.Slug == CitySlug)
                           : null)
                       ?? Cities.FirstOrDefault(c => c.IsDefault)
                       ?? Cities[0];

        TodayLocal = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3.5));

        // ---------- امروز ----------
        Today = await prayerTimes.GetForDayAsync(SelectedCity.Id, TodayLocal, ct);

        var nowLocal = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3.5));
        var next = Today.NextAfter(nowLocal);
        if (next is not null)
        {
            NextPrayerName = next.Value.Name;
            var targetUtc = TodayLocal.ToDateTime(next.Value.At).AddHours(-3.5);
            TimeUntilNext = dates.HumanizeUntil(DateTime.SpecifyKind(targetUtc, DateTimeKind.Utc));
        }

        // ---------- ماه انتخابی ----------
        var todayGregorian = TodayLocal.ToDateTime(TimeOnly.MinValue);
        PersianYear = Year ?? Persian.GetYear(todayGregorian);
        PersianMonth = Month ?? Persian.GetMonth(todayGregorian);

        if (PersianMonth is < 1 or > 12) PersianMonth = Persian.GetMonth(todayGregorian);
        if (PersianYear is < 1300 or > 1500) PersianYear = Persian.GetYear(todayGregorian);

        MonthTitle = $"{PersianMonths[PersianMonth - 1]} {dates.ToPersianDigits(PersianYear.ToString())}";

        var daysInMonth = Persian.GetDaysInMonth(PersianYear, PersianMonth);
        var firstDay = DateOnly.FromDateTime(Persian.ToDateTime(PersianYear, PersianMonth, 1, 0, 0, 0, 0));
        var lastDay = firstDay.AddDays(daysInMonth - 1);

        MonthDays = await prayerTimes.GetForRangeAsync(SelectedCity.Id, firstDay, lastDay, ct);

        var monthOccasions = await occasions.GetInRangeAsync(firstDay, lastDay, ct);
        MonthOccasions = monthOccasions
            .GroupBy(o => o.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        return Page();
    }

    /// <summary>ماه قبل و بعد، با در نظر گرفتن تغییر سال.</summary>
    public (int Year, int Month) PreviousMonth =>
        PersianMonth == 1 ? (PersianYear - 1, 12) : (PersianYear, PersianMonth - 1);

    public (int Year, int Month) NextMonth =>
        PersianMonth == 12 ? (PersianYear + 1, 1) : (PersianYear, PersianMonth + 1);

    public int PersianDayOf(DateOnly date) =>
        Persian.GetDayOfMonth(date.ToDateTime(TimeOnly.MinValue));

    public string WeekdayOf(DateOnly date)
    {
        var names = new[] { "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه" };
        return names[(int)date.DayOfWeek];
    }

    public bool IsFriday(DateOnly date) => date.DayOfWeek == DayOfWeek.Friday;
}