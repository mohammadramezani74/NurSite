using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Services;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Pages.Southa;

/// <summary>
/// فهرست یک بخش صوتی. هر سه بخش از همین صفحه سرو می‌شوند و بخشِ
/// نشانی تعیین می‌کند کدام نوع نمایش داده شود.
/// </summary>
public class IndexModel(AppDbContext db) : PageModel
{
    private const int PageSize = 12;

    public AudioKind Kind { get; private set; }
    public string SectionSlug { get; private set; } = "";

    public IReadOnlyList<Lecture> Items { get; private set; } = [];
    public IReadOnlyList<Speaker> Speakers { get; private set; } = [];
    public IReadOnlyList<LectureSeries> SeriesList { get; private set; } = [];

    public Speaker? ActiveSpeaker { get; private set; }
    public LectureSeries? ActiveSeries { get; private set; }

    [BindProperty(SupportsGet = true, Name = "q")] public string? Query { get; set; }
    [BindProperty(SupportsGet = true, Name = "goyande")] public string? SpeakerSlug { get; set; }
    [BindProperty(SupportsGet = true, Name = "majmooe")] public string? SeriesSlug { get; set; }
    [BindProperty(SupportsGet = true, Name = "safhe")] public int PageNumber { get; set; } = 1;

    public int TotalCount { get; private set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public string BaseUrl { get; private set; } = "";

    /// <summary>شماره صفحه‌ها با سه‌نقطه. null یعنی چند صفحه اینجا حذف شده.</summary>
    public IReadOnlyList<int?> PagerPages => Pager.Pages(PageNumber, TotalPages);

    /// <summary>فیلتری روشن است؟ برای نمایش دکمه «حذف فیلترها».</summary>
    public bool HasFilter =>
        !string.IsNullOrWhiteSpace(Query) || ActiveSpeaker is not null || ActiveSeries is not null;

    public async Task<IActionResult> OnGetAsync(string section, CancellationToken ct)
    {
        var kind = AudioKinds.FromSectionSlug(section);
        if (kind is null) return NotFound();

        Kind = kind.Value;
        SectionSlug = AudioKinds.SectionSlug(Kind);

        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        BaseUrl = (settings?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');

        var query = db.Lectures.AsNoTracking()
            .Include(l => l.Speaker)
            .Include(l => l.LectureSeries)
            .Where(l => l.Kind == Kind && l.Status == PublishStatus.Published);

        // فقط گویندگان و مجموعه‌هایی که در همین بخش اثری دارند، وگرنه
        // کاربر روی فیلتری کلیک می‌کند که نتیجه‌اش خالی است
        var speakerIds = await query.Where(l => l.SpeakerId != null)
            .Select(l => l.SpeakerId!.Value).Distinct().ToListAsync(ct);

        Speakers = await db.Speakers.AsNoTracking()
            .Where(s => speakerIds.Contains(s.Id))
            .OrderBy(s => s.FullName).ToListAsync(ct);

        var seriesIds = await query.Where(l => l.LectureSeriesId != null)
            .Select(l => l.LectureSeriesId!.Value).Distinct().ToListAsync(ct);

        SeriesList = await db.LectureSeries.AsNoTracking()
            .Where(s => seriesIds.Contains(s.Id))
            .OrderBy(s => s.Title).ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(SpeakerSlug))
        {
            ActiveSpeaker = Speakers.FirstOrDefault(s => s.Slug == SpeakerSlug);
            if (ActiveSpeaker is null) return NotFound();

            query = query.Where(l => l.SpeakerId == ActiveSpeaker.Id);
        }

        if (!string.IsNullOrWhiteSpace(SeriesSlug))
        {
            ActiveSeries = SeriesList.FirstOrDefault(s => s.Slug == SeriesSlug);
            if (ActiveSeries is null) return NotFound();

            query = query.Where(l => l.LectureSeriesId == ActiveSeries.Id);
        }

        // جستجو داخل همین بخش. روی ستون یکسان‌شده انجام می‌شود تا
        // «مداحی» و «مداحى» یک نتیجه بدهند، و منطقش AND است نه OR،
        // وگرنه هر واژه‌ای نصف آرشیو را برمی‌گرداند.
        var terms = PersianText.Tokenize(Query);
        foreach (var term in terms)
        {
            var t = term;
            query = query.Where(l => l.SearchText != null && l.SearchText.Contains(t));
        }

        TotalCount = await query.CountAsync(ct);

        if (PageNumber < 1) PageNumber = 1;
        if (TotalPages > 0 && PageNumber > TotalPages) return NotFound();

        if (TotalCount == 0) return Page();

        // در یک مجموعه، ترتیب جلسه مهم‌تر از تاریخ انتشار است
        query = ActiveSeries is null
            ? query.OrderByDescending(l => l.PublishedAtUtc)
            : query.OrderBy(l => l.EpisodeNumber).ThenBy(l => l.PublishedAtUtc);

        Items = await query
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);

        return Page();
    }

    /// <summary>صفحه نتیجه جستجو محتوای پایدار ندارد و نباید ایندکس شود.</summary>
    public bool NoIndex => !string.IsNullOrWhiteSpace(Query);

    public string PageTitle => ActiveSeries?.Title
        ?? (ActiveSpeaker is null
            ? AudioKinds.PluralLabel(Kind)
            : $"{AudioKinds.PluralLabel(Kind)} {ActiveSpeaker.FullName}");

    public string PageDescription => ActiveSeries?.MetaDescription
        ?? ActiveSpeaker?.Bio
        ?? Kind switch
        {
            AudioKind.Madahi => "مداحی‌ها و نوحه‌های مؤسسه فرهنگی نورالثقلین، برای شنیدن و دانلود.",
            AudioKind.Anthem => "سرودها و آهنگ‌های مذهبی مؤسسه فرهنگی نورالثقلین، برای شنیدن و دانلود.",
            _ => "سخنرانی‌ها و درس‌گفتارهای مؤسسه فرهنگی نورالثقلین، برای شنیدن و دانلود."
        };

    /// <summary>فهرست صوت‌ها به شکل ساختاریافته، تا گوگل بداند این صفحه یک آرشیو است.</summary>
    public object BuildItemListSchema() => new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "ItemList",
        ["name"] = PageTitle,
        ["numberOfItems"] = TotalCount,
        ["itemListElement"] = Items.Select((item, index) => new Dictionary<string, object?>
        {
            ["@type"] = "ListItem",
            ["position"] = (PageNumber - 1) * PageSize + index + 1,
            ["name"] = item.Title,
            ["url"] = $"{BaseUrl}{AudioKinds.Url(item.Kind, item.Slug)}"
        }).ToList()
    };
}