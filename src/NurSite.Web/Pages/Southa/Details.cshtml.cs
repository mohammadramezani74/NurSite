using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Services;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Pages.Southa;

public class DetailsModel(AppDbContext db) : PageModel
{
    public Lecture Item { get; private set; } = default!;
    public IReadOnlyList<Lecture> Related { get; private set; } = [];

    public string SectionSlug { get; private set; } = "";
    public string BaseUrl { get; private set; } = "";
    public string CanonicalUrl { get; private set; } = "";

    /// <summary>عنوان بخشی که این صوت زیرش می‌نشیند. برای مسیر راهنما.</summary>
    public string SectionTitle => AudioKinds.PluralLabel(Item.Kind);

    public async Task<IActionResult> OnGetAsync(string section, string slug, CancellationToken ct)
    {
        var kind = AudioKinds.FromSectionSlug(section);
        if (kind is null || string.IsNullOrWhiteSpace(slug)) return NotFound();

        var item = await db.Lectures.AsNoTracking()
            .Include(l => l.Speaker)
            .Include(l => l.LectureSeries)
            .FirstOrDefaultAsync(l => l.Slug == slug && l.Status == PublishStatus.Published, ct);

        if (item is null) return NotFound();

        // اسلاگ یکتاست، پس صوت پیدا می‌شود حتی اگر بخشِ نشانی اشتباه باشد.
        // در آن حالت به نشانی درست هدایت می‌کنیم تا دو نشانی برای یک
        // محتوا وجود نداشته باشد و امتیاز صفحه دو تکه نشود.
        if (item.Kind != kind.Value)
            return RedirectToPagePermanent("/Southa/Details", new
            {
                section = AudioKinds.SectionSlug(item.Kind),
                slug = item.Slug
            });

        Item = item;
        SectionSlug = AudioKinds.SectionSlug(item.Kind);

        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        BaseUrl = (settings?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        CanonicalUrl = $"{BaseUrl}{AudioKinds.Url(item.Kind, item.Slug)}";

        // هم‌مجموعه‌ای‌ها مرتبط‌ترند؛ اگر مجموعه ندارد، آثار دیگر همان گوینده
        var related = db.Lectures.AsNoTracking()
            .Include(l => l.Speaker)
            .Where(l => l.Status == PublishStatus.Published && l.Id != item.Id);

        Related = item.LectureSeriesId is not null
            ? await related.Where(l => l.LectureSeriesId == item.LectureSeriesId)
                .OrderBy(l => l.EpisodeNumber).Take(6).ToListAsync(ct)
            : await related.Where(l => l.Kind == item.Kind &&
                                       (item.SpeakerId == null || l.SpeakerId == item.SpeakerId))
                .OrderByDescending(l => l.PublishedAtUtc).Take(4).ToListAsync(ct);

        return Page();
    }

    /// <summary>
    /// دانلود اجازه دارد؟ فایل بیرونی همیشه در دسترس است چون نشانی‌اش
    /// دست ما نیست و پنهان کردنش فقط ظاهری می‌شد.
    /// </summary>
    public bool CanDownload => Item.IsExternal || Item.DownloadAccess switch
    {
        DownloadAccess.Everyone => true,
        DownloadAccess.SignedIn => User.Identity?.IsAuthenticated == true,
        _ => false
    };

    /// <summary>دانلود ممکن است ولی کاربر باید اول وارد شود.</summary>
    public bool DownloadNeedsLogin =>
        !Item.IsExternal &&
        Item.DownloadAccess == DownloadAccess.SignedIn &&
        User.Identity?.IsAuthenticated != true;

    /// <summary>نشانی نسبی همین صفحه، برای بازگشت بعد از ورود.</summary>
    public string ReturnPath => AudioKinds.Url(Item.Kind, Item.Slug);

    public string DownloadUrl => Item.IsExternal
        ? Item.ExternalAudioUrl!
        : $"/danlod/{Item.Id}";

    /// <summary>حجم فایل به شکل خوانا. مثال: ۱۲٫۴ مگابایت</summary>
    public string FileSizeLabel
    {
        get
        {
            if (Item.FileSizeBytes <= 0) return "";
            var mb = Item.FileSizeBytes / 1024d / 1024d;
            return mb >= 1
                ? $"{mb:0.#} مگابایت"
                : $"{Item.FileSizeBytes / 1024d:0} کیلوبایت";
        }
    }

    /// <summary>مدت به قالب ISO 8601، همان چیزی که schema.org می‌خواهد. مثال: PT1H5M30S</summary>
    private string? IsoDuration
    {
        get
        {
            if (Item.DurationSeconds <= 0) return null;

            var span = TimeSpan.FromSeconds(Item.DurationSeconds);
            var value = "PT";
            if (span.Hours > 0 || span.Days > 0) value += $"{(int)span.TotalHours}H";
            if (span.Minutes > 0) value += $"{span.Minutes}M";
            if (span.Seconds > 0) value += $"{span.Seconds}S";
            return value;
        }
    }

    public object BuildAudioSchema()
    {
        var schema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "AudioObject",
            ["name"] = Item.Title,
            ["description"] = Item.MetaDescription,
            ["duration"] = IsoDuration,
            ["encodingFormat"] = Item.IsExternal ? null : "audio/mpeg",
            ["uploadDate"] = Item.PublishedAtUtc?.ToString("o"),
            ["inLanguage"] = "fa-IR",
            ["url"] = CanonicalUrl,
            ["thumbnailUrl"] = string.IsNullOrWhiteSpace(Item.OgImagePath)
                ? null
                : $"{BaseUrl}{Item.OgImagePath}",
            ["publisher"] = new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["name"] = "مؤسسه فرهنگی نورالثقلین",
                ["logo"] = new Dictionary<string, object>
                {
                    ["@type"] = "ImageObject",
                    ["url"] = $"{BaseUrl}/icons/icon-512.png"
                }
            }
        };

        if (Item.Speaker is not null)
            schema["author"] = new Dictionary<string, object?>
            {
                ["@type"] = "Person",
                ["name"] = Item.Speaker.FullName,
                ["jobTitle"] = Item.Speaker.Title
            };

        if (Item.LectureSeries is not null)
            schema["partOfSeries"] = new Dictionary<string, object?>
            {
                ["@type"] = "CreativeWorkSeries",
                ["name"] = Item.LectureSeries.Title,
                ["url"] = $"{BaseUrl}/{SectionSlug}?majmooe={Item.LectureSeries.Slug}"
            };

        // نشانی مستقیم فایل فقط وقتی اعلام می‌شود که واقعاً برای همه باز است.
        // اگر دانلود محدود یا بسته است، اعلامش یعنی دور زدن همان محدودیت.
        if (Item.DownloadAccess == DownloadAccess.Everyone && !string.IsNullOrWhiteSpace(Item.AudioUrl))
        {
            schema["contentUrl"] = Item.IsExternal
                ? Item.ExternalAudioUrl
                : $"{BaseUrl}{Item.AudioPath}";
            if (Item.FileSizeBytes > 0) schema["contentSize"] = $"{Item.FileSizeBytes}";
        }
        else
        {
            schema["embedUrl"] = CanonicalUrl;
        }

        return schema;
    }

    public object BuildBreadcrumbSchema()
    {
        var items = new List<Dictionary<string, object>>
        {
            new()
            {
                ["@type"] = "ListItem", ["position"] = 1,
                ["name"] = "خانه", ["item"] = BaseUrl
            },
            new()
            {
                ["@type"] = "ListItem", ["position"] = 2,
                ["name"] = SectionTitle, ["item"] = $"{BaseUrl}/{SectionSlug}"
            }
        };

        if (Item.LectureSeries is not null)
            items.Add(new Dictionary<string, object>
            {
                ["@type"] = "ListItem",
                ["position"] = 3,
                ["name"] = Item.LectureSeries.Title,
                ["item"] = $"{BaseUrl}/{SectionSlug}?majmooe={Item.LectureSeries.Slug}"
            });

        items.Add(new Dictionary<string, object>
        {
            ["@type"] = "ListItem",
            ["position"] = items.Count + 1,
            ["name"] = Item.Title,
            ["item"] = CanonicalUrl
        });

        return new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = items
        };
    }
}