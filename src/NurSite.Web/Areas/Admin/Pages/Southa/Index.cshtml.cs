using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;
using NurSite.Web.Services;

namespace NurSite.Web.Areas.Admin.Pages.Southa;

/// <summary>
/// فهرست همه صوت‌ها — سخنرانی، مداحی و سرود در یک جدول با فیلتر نوع.
/// در سایت هر نوع بخش جدا دارد، ولی در پنل یک جا مدیریت می‌شوند.
/// </summary>
public class IndexModel(AppDbContext db, FileUploadService uploads) : PageModel
{
    private const int PageSize = 20;

    public IReadOnlyList<Lecture> Items { get; private set; } = [];
    public IReadOnlyList<Speaker> Speakers { get; private set; } = [];
    public IReadOnlyList<LectureSeries> SeriesList { get; private set; } = [];

    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true)] public AudioKind? Kind { get; set; }
    [BindProperty(SupportsGet = true)] public PublishStatus? Status { get; set; }
    [BindProperty(SupportsGet = true)] public int? SpeakerId { get; set; }
    [BindProperty(SupportsGet = true)] public int? SeriesId { get; set; }

    // نام «page» رزرو شده است و مسیر خود صفحه را حمل می‌کند
    [BindProperty(SupportsGet = true, Name = "safhe")] public int PageNumber { get; set; } = 1;

    public int TotalCount { get; private set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Speakers = await db.Speakers.AsNoTracking()
            .OrderBy(s => s.FullName).ToListAsync(ct);

        SeriesList = await db.LectureSeries.AsNoTracking()
            .OrderBy(s => s.Title).ToListAsync(ct);

        var query = db.Lectures.AsNoTracking()
            .Include(l => l.Speaker)
            .Include(l => l.LectureSeries)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = Q.Trim();
            query = query.Where(l => l.Title.Contains(term) ||
                                     (l.Description != null && l.Description.Contains(term)));
        }

        if (Kind is not null) query = query.Where(l => l.Kind == Kind);
        if (Status is not null) query = query.Where(l => l.Status == Status);
        if (SpeakerId is not null) query = query.Where(l => l.SpeakerId == SpeakerId);
        if (SeriesId is not null) query = query.Where(l => l.LectureSeriesId == SeriesId);

        TotalCount = await query.CountAsync(ct);

        if (PageNumber < 1) PageNumber = 1;
        if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

        Items = await query
            // در یک مجموعه، ترتیب جلسه مهم‌تر از تاریخ است
            .OrderByDescending(l => l.LectureSeriesId != null)
            .ThenBy(l => l.LectureSeriesId)
            .ThenBy(l => l.EpisodeNumber)
            .ThenByDescending(l => l.UpdatedAtUtc ?? l.CreatedAtUtc)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, CancellationToken ct)
    {
        var item = await db.Lectures.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (item is null) return NotFound();

        if (item.Status == PublishStatus.Published)
        {
            item.Status = PublishStatus.Draft;
            Flash = $"«{item.Title}» به پیش‌نویس برگشت.";
        }
        else
        {
            // بدون فایل صوت، صفحه عمومی چیزی برای پخش ندارد
            if (string.IsNullOrWhiteSpace(item.AudioUrl))
            {
                Flash = $"«{item.Title}» هنوز فایل صوتی ندارد و منتشر نمی‌شود.";
                FlashKind = "warn";
                return RedirectToCurrent();
            }

            item.Status = PublishStatus.Published;
            item.PublishedAtUtc ??= DateTime.UtcNow;
            Flash = $"«{item.Title}» منتشر شد.";
        }

        FlashKind ??= "ok";
        await db.SaveChangesAsync(ct);
        return RedirectToCurrent();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var item = await db.Lectures.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (item is null) return NotFound();

        // حذف نرم است، پس فایل صوت را نگه می‌داریم تا اگر بازگردانی شد از دست نرود
        db.Lectures.Remove(item);
        await db.SaveChangesAsync(ct);

        Flash = $"«{item.Title}» حذف شد.";
        FlashKind = "ok";
        return RedirectToCurrent();
    }

    private IActionResult RedirectToCurrent() =>
        RedirectToPage(new { Q, Kind, Status, SpeakerId, SeriesId, safhe = PageNumber });
}