using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.DTOs;
using NurSite.Application.Interfaces;
using NurSite.Application.Services;
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
    /// <summary>چند صوت در جعبه کنار صفحه اصلی جا می‌شود.</summary>
    private const int AudioBoxSize = 6;

    /// <summary>صوت‌های جعبه صفحه اصلی — دستچین مدیر، و بعد تازه‌ترین‌ها.</summary>
    public IReadOnlyList<Lecture> LatestAudio { get; private set; } = [];
    public IReadOnlyList<Ruling> FaqRulings { get; private set; } = [];

    /// <summary>درخت نمودار هر حکم نموداری، بر اساس شناسه حکم.</summary>
    public IReadOnlyDictionary<int, IReadOnlyList<RulingNode>> FaqDiagrams { get; private set; }
        = new Dictionary<int, IReadOnlyList<RulingNode>>();

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

        LatestAudio = await LoadAudioBoxAsync(ct);

        FaqRulings = await db.Rulings.AsNoTracking()
            .Where(r => r.Status == PublishStatus.Published && r.IsFrequentlyAsked)
            .OrderBy(r => r.SortOrder)
            .Take(4)
            .ToListAsync(ct);

        // احکام نموداری متن پاسخ ندارند، پس درختشان جدا خوانده می‌شود
        var diagramIds = FaqRulings.Where(r => r.HasDiagram).Select(r => r.Id).ToList();
        if (diagramIds.Count > 0)
        {
            var nodes = await db.RulingNodes.AsNoTracking()
                .Where(n => diagramIds.Contains(n.RulingId))
                .Include(n => n.Verdicts).ThenInclude(v => v.Marjas).ThenInclude(m => m.Marja)
                .OrderBy(n => n.Depth).ThenBy(n => n.SortOrder)
                .ToListAsync(ct);

            var byId = nodes.ToDictionary(n => n.Id);
            foreach (var node in nodes)
            {
                if (node.ParentId is not null && byId.TryGetValue(node.ParentId.Value, out var parent))
                    parent.Children.Add(node);
            }

            FaqDiagrams = nodes
                .GroupBy(n => n.RulingId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<RulingNode>)g.ToList());
        }
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
                ["text"] = AnswerTextOf(r)
            }
        }).ToList()
    };

    /// <summary>
    /// متن پاسخ برای نشانه‌گذاری. در احکام نموداری، درخت به متن خطی
    /// تبدیل می‌شود چون پاسخ خالی کل نشانه‌گذاری را بی‌اثر می‌کند.
    /// </summary>
    private string AnswerTextOf(Ruling r)
    {
        if (!r.HasDiagram || !FaqDiagrams.TryGetValue(r.Id, out var nodes))
            return StripHtml(r.Answer);

        var sb = new System.Text.StringBuilder();

        void Walk(IEnumerable<RulingNode> items)
        {
            foreach (var n in items.OrderBy(x => x.SortOrder))
            {
                sb.Append(n.Text);
                foreach (var v in n.Verdicts.OrderBy(x => x.SortOrder))
                    sb.Append(' ').Append(v.Text);
                sb.Append(". ");
                Walk(n.Children);
            }
        }

        Walk(nodes.Where(n => n.ParentId is null));
        return sb.ToString().Trim();
    }

    internal static string StripHtml(string? html) =>
        string.IsNullOrWhiteSpace(html)
            ? string.Empty
            : System.Net.WebUtility.HtmlDecode(
                System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ")).Trim();

    /// <summary>
    /// محتوای جعبه آرشیو صوتی صفحه اصلی.
    ///
    /// اگر مدیر چیزی را ستاره زده باشد، فقط همان‌ها — حتی اگر یکی باشد.
    /// پر کردن جای خالی با تازه‌ترین‌ها یعنی مدیر ستاره را بزند و هیچ
    /// تغییری در صفحه نبیند، که یعنی آن دکمه از نظر او کار نمی‌کند.
    /// </summary>
    private async Task<IReadOnlyList<Lecture>> LoadAudioBoxAsync(CancellationToken ct)
    {
        var featured = await db.Lectures.AsNoTracking()
            .Include(l => l.Speaker)
            .Where(l => l.IsFeatured && l.Status == PublishStatus.Published)
            .OrderByDescending(l => l.PublishedAtUtc)
            .Take(AudioBoxSize)
            .ToListAsync(ct);

        if (featured.Count > 0) return featured;

        // هیچ ستاره‌ای نخورده: از هر نوع تازه‌ترین‌ها. نه چند تای آخر
        // بدون توجه به نوع، وگرنه یک شب محرم که ده مداحی منتشر می‌شود
        // سخنرانی‌ها کلاً از صفحه اصلی محو می‌شوند.
        var audio = new List<Lecture>();
        foreach (var kind in AudioKinds.All)
        {
            audio.AddRange(await db.Lectures.AsNoTracking()
                .Include(l => l.Speaker)
                .Where(l => l.Kind == kind && l.Status == PublishStatus.Published)
                .OrderByDescending(l => l.PublishedAtUtc)
                .Take(2)
                .ToListAsync(ct));
        }

        return audio.Take(AudioBoxSize).ToList();
    }

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