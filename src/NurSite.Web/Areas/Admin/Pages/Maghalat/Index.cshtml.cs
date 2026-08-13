using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;
using NurSite.Web.Services;

namespace NurSite.Web.Areas.Admin.Pages.Maghalat;

public class IndexModel(AppDbContext db, FileUploadService uploads) : PageModel
{
    private const int PageSize = 15;

    public IReadOnlyList<Article> Articles { get; private set; } = [];
    public IReadOnlyList<Category> Categories { get; private set; } = [];

    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true)] public PublishStatus? Status { get; set; }
    [BindProperty(SupportsGet = true)] public int? CategoryId { get; set; }
    [BindProperty(SupportsGet = true, Name = "safhe")] public int PageNumber { get; set; } = 1;

    public int TotalCount { get; private set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Categories = await db.Categories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
            .ToListAsync(ct);

        var query = db.Articles.AsNoTracking().Include(a => a.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = Q.Trim();
            query = query.Where(a => a.Title.Contains(term) || (a.Summary != null && a.Summary.Contains(term)));
        }

        if (Status is not null) query = query.Where(a => a.Status == Status);
        if (CategoryId is not null) query = query.Where(a => a.CategoryId == CategoryId);

        TotalCount = await query.CountAsync(ct);

        if (PageNumber < 1) PageNumber = 1;
        if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

        Articles = await query
            .OrderByDescending(a => a.UpdatedAtUtc ?? a.CreatedAtUtc)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);
    }

    /// <summary>تغییر سریع وضعیت انتشار بدون باز کردن فرم ویرایش.</summary>
    public async Task<IActionResult> OnPostToggleAsync(int id, CancellationToken ct)
    {
        var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (article is null) return NotFound();

        if (article.Status == PublishStatus.Published)
        {
            article.Status = PublishStatus.Draft;
            Flash = $"«{article.Title}» به پیش‌نویس برگشت.";
        }
        else
        {
            article.Status = PublishStatus.Published;
            article.PublishedAtUtc ??= DateTime.UtcNow;
            Flash = $"«{article.Title}» منتشر شد.";
        }

        FlashKind = "ok";
        await db.SaveChangesAsync(ct);
        return RedirectToPage(new { Q, Status, CategoryId, safhe = PageNumber });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (article is null) return NotFound();

        // حذف نرم است، پس فایل تصویر را نگه می‌داریم تا اگر بازگردانی شد از دست نرود
        db.Articles.Remove(article);
        await db.SaveChangesAsync(ct);

        Flash = $"«{article.Title}» حذف شد.";
        FlashKind = "ok";
        return RedirectToPage(new { Q, Status, CategoryId, safhe = PageNumber });
    }
}