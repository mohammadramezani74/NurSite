using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.DTOs;
using NurSite.Application.Interfaces;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Pages;

public class IndexModel(
    AppDbContext db,
    IPrayerTimeService prayerTimes,
    IPersianDateService dates) : PageModel
{
    public IReadOnlyList<HeroVerse> Verses { get; private set; } = [];
    public PrayerTimesDto? Times { get; private set; }
    public string? NextPrayerName { get; private set; }
    public string? TimeUntilNext { get; private set; }
    public IReadOnlyList<City> Cities { get; private set; } = [];
    public int SelectedCityId { get; private set; }
    public IReadOnlyList<Event> UpcomingEvents { get; private set; } = [];
    public IReadOnlyList<Article> LatestArticles { get; private set; } = [];
    public IReadOnlyList<Lecture> LatestLectures { get; private set; } = [];
    public IReadOnlyList<Ruling> FaqRulings { get; private set; } = [];

    /// <summary>آدرس مبنای سایت — برای ساخت لینک‌های مطلق در نشانه‌گذاری ساختاریافته.</summary>
    public string BaseUrl { get; private set; } = string.Empty;

    public async Task OnGetAsync(int? city, CancellationToken ct)
    {
        BaseUrl = $"{Request.Scheme}://{Request.Host}";

        Verses = await db.HeroVerses.AsNoTracking()
            .Where(v => v.IsActive)
            .OrderBy(v => v.SortOrder)
            .ToListAsync(ct);

        Cities = await db.Cities.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

        SelectedCityId = city
            ?? Cities.FirstOrDefault(c => c.IsDefault)?.Id
            ?? Cities.FirstOrDefault()?.Id
            ?? 0;

        if (SelectedCityId > 0)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3.5));
            Times = await prayerTimes.GetForDayAsync(SelectedCityId, today, ct);

            var nowLocal = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3.5));
            var next = Times.NextAfter(nowLocal);
            if (next is not null)
            {
                NextPrayerName = next.Value.Name;
                var target = today.ToDateTime(next.Value.At).AddHours(-3.5);
                TimeUntilNext = dates.HumanizeUntil(DateTime.SpecifyKind(target, DateTimeKind.Utc));
            }
        }

        UpcomingEvents = await db.Events.AsNoTracking()
            .Where(e => e.Status == PublishStatus.Published && e.StartsAtUtc >= DateTime.UtcNow)
            .OrderBy(e => e.StartsAtUtc)
            .Take(3)
            .ToListAsync(ct);

        LatestArticles = await db.Articles.AsNoTracking()
            .Include(a => a.Category)
            .Where(a => a.Status == PublishStatus.Published)
            .OrderByDescending(a => a.PublishedAtUtc)
            .Take(3)
            .ToListAsync(ct);

        LatestLectures = await db.Lectures.AsNoTracking()
            .Include(l => l.Speaker)
            .Where(l => l.Status == PublishStatus.Published)
            .OrderByDescending(l => l.PublishedAtUtc)
            .Take(4)
            .ToListAsync(ct);

        FaqRulings = await db.Rulings.AsNoTracking()
            .Where(r => r.Status == PublishStatus.Published && r.IsFrequentlyAsked)
            .OrderBy(r => r.SortOrder)
            .Take(4)
            .ToListAsync(ct);
    }

    /// <summary>
    /// نشانه‌گذاری FAQPage. باعث می‌شود پرسش و پاسخ‌ها مستقیم در نتایج گوگل نمایش داده شوند.
    /// از دیکشنری استفاده می‌کنیم چون کلیدهای اسکیما با @ شروع می‌شوند
    /// و نام خاصیت در سی‌شارپ نمی‌تواند آن شکل را داشته باشد.
    /// </summary>
    public object BuildFaqSchema() => new Dictionary<string, object>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "FAQPage",
        ["mainEntity"] = FaqRulings.Select(r => new Dictionary<string, object>
        {
            ["@type"] = "Question",
            ["name"] = r.Question,
            ["acceptedAnswer"] = new Dictionary<string, object>
            {
                ["@type"] = "Answer",
                ["text"] = r.Answer
            }
        }).ToList()
    };

    public object BuildOrganizationSchema() => new Dictionary<string, object>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "Organization",
        ["name"] = "مؤسسه فرهنگی نورالثقلین",
        ["url"] = BaseUrl,
        ["logo"] = $"{BaseUrl}/icons/icon-512.png",
        ["address"] = new Dictionary<string, object>
        {
            ["@type"] = "PostalAddress",
            ["addressLocality"] = "تهران",
            ["addressCountry"] = "IR"
        }
    };

    /// <summary>نشانه‌گذاری برنامه‌ها — برای نمایش در نتایج رویدادهای گوگل.</summary>
    public IEnumerable<object> BuildEventSchemas() =>
        UpcomingEvents.Select(e => new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Event",
            ["name"] = e.Title,
            ["startDate"] = e.StartsAtUtc.ToString("o"),
            ["endDate"] = e.EndsAtUtc?.ToString("o"),
            ["eventAttendanceMode"] = "https://schema.org/OfflineEventAttendanceMode",
            ["eventStatus"] = "https://schema.org/EventScheduled",
            ["url"] = $"{BaseUrl}/barnameh/{e.Slug}",
            ["description"] = e.Summary,
            ["location"] = new Dictionary<string, object?>
            {
                ["@type"] = "Place",
                ["name"] = e.LocationName,
                ["address"] = e.LocationAddress
            }
        });
}