using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Pages.Maghalat;

public class IndexModel(AppDbContext db) : PageModel
{
    private const int PageSize = 9;

    public IReadOnlyList<Article> Articles { get; private set; } = [];
    public IReadOnlyList<Category> Categories { get; private set; } = [];
    public Category? ActiveCategory { get; private set; }

    [BindProperty(SupportsGet = true, Name = "dasteh")] public string? CategorySlug { get; set; }
    [BindProperty(SupportsGet = true, Name = "page")] public int PageNumber { get; set; } = 1;

    public int TotalCount { get; private set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        Categories = await db.Categories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
            .ToListAsync(ct);

        var query = db.Articles.AsNoTracking()
            .Include(a => a.Category)
            .Where(a => a.Status == PublishStatus.Published);

        if (!string.IsNullOrWhiteSpace(CategorySlug))
        {
            ActiveCategory = Categories.FirstOrDefault(c => c.Slug == CategorySlug);
            if (ActiveCategory is null) return NotFound();

            query = query.Where(a => a.CategoryId == ActiveCategory.Id);
        }

        TotalCount = await query.CountAsync(ct);

        if (PageNumber < 1) PageNumber = 1;
        if (TotalPages > 0 && PageNumber > TotalPages) return NotFound();

        Articles = await query
            .OrderByDescending(a => a.PublishedAtUtc)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);

        return Page();
    }
}