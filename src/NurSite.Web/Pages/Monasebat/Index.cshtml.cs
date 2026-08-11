using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.DTOs;
using NurSite.Application.Interfaces;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Pages.Monasebat;

public class IndexModel(
    AppDbContext db,
    IOccasionService occasions,
    IPersianDateService dates) : PageModel
{
    private static readonly UmAlQuraCalendar Hijri = new();

    private static readonly string[] HijriMonths =
    {
        "محرم","صفر","ربیع‌الأول","ربیع‌الثانی","جمادی‌الأول","جمادی‌الثانی",
        "رجب","شعبان","رمضان","شوال","ذی‌القعده","ذی‌الحجه"
    };

    /// <summary>مناسبت‌های یک سال قمری کامل، گروه‌بندی‌شده بر اساس ماه قمری.</summary>
    public IReadOnlyList<IGrouping<string, OccasionOccurrence>> Groups { get; private set; } = [];

    public IReadOnlyList<OccasionOccurrence> Upcoming { get; private set; } = [];
    public OccasionOccurrence? Next { get; private set; }

    public string HijriYearLabel { get; private set; } = "";
    public string BaseUrl { get; private set; } = "";
    public int TotalCount { get; private set; }

    [BindProperty(SupportsGet = true, Name = "noe")] public OccasionKind? Kind { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var siteSetting = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        BaseUrl = (siteSetting?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3.5));

        // یک سال قمری از امروز به بعد — حدود ۳۵۴ روز
        var all = await occasions.GetInRangeAsync(today, today.AddDays(360), ct);

        if (Kind is not null)
            all = all.Where(o => o.Kind == Kind).ToList();

        TotalCount = all.Count;
        Upcoming = all.Take(4).ToList();
        Next = all.FirstOrDefault();

        Groups = all
            .GroupBy(o => HijriMonths[o.HijriMonth - 1])
            .ToList();

        var hijriYear = Hijri.GetYear(today.ToDateTime(TimeOnly.MinValue));
        HijriYearLabel = dates.ToPersianDigits(hijriYear.ToString());
    }

    public string HijriDateOf(OccasionOccurrence o) =>
        dates.ToPersianDigits($"{o.HijriDay} {HijriMonths[o.HijriMonth - 1]}");

    public string CountdownOf(OccasionOccurrence o)
    {
        var days = o.DaysFromToday;
        return days switch
        {
            0 => "امروز",
            1 => "فردا",
            _ => dates.ToPersianDigits($"{days} روز مانده")
        };
    }

    /// <summary>نشانه‌گذاری رویداد برای مناسبت‌های نزدیک.</summary>
    public IEnumerable<object> BuildEventSchemas() =>
        Upcoming.Select(o => new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Event",
            ["name"] = o.Title,
            ["startDate"] = o.Date.ToString("yyyy-MM-dd"),
            ["endDate"] = o.Date.ToString("yyyy-MM-dd"),
            ["eventAttendanceMode"] = "https://schema.org/OfflineEventAttendanceMode",
            ["eventStatus"] = "https://schema.org/EventScheduled",
            ["description"] = o.Description,
            ["url"] = $"{BaseUrl}/monasebat",
            ["organizer"] = new Dictionary<string, object>
            {
                ["@type"] = "Organization",
                ["name"] = "مؤسسه فرهنگی نورالثقلین",
                ["url"] = BaseUrl
            }
        });
}