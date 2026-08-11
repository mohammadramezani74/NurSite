using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Services;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Pages.Ahkam;

public class IndexModel(AppDbContext db) : PageModel
{
    public IReadOnlyList<RulingCategory> Categories { get; private set; } = [];
    public RulingCategory? ActiveCategory { get; private set; }

    /// <summary>احکام گروه‌بندی‌شده بر اساس باب، برای نمایش آکاردئونی.</summary>
    public IReadOnlyList<IGrouping<string, Ruling>> Groups { get; private set; } = [];

    public int TotalCount { get; private set; }
    public string BaseUrl { get; private set; } = "";

    [BindProperty(SupportsGet = true, Name = "bab")] public string? CategorySlug { get; set; }
    [BindProperty(SupportsGet = true, Name = "q")] public string? Query { get; set; }

    public bool HasQuery => !string.IsNullOrWhiteSpace(Query);

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var siteSetting = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        BaseUrl = (siteSetting?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');

        Categories = await db.RulingCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
            .ToListAsync(ct);

        var query = db.Rulings.AsNoTracking()
            .Include(r => r.RulingCategory)
            .Include(r => r.Marja)
            .Where(r => r.Status == PublishStatus.Published);

        if (!string.IsNullOrWhiteSpace(CategorySlug))
        {
            ActiveCategory = Categories.FirstOrDefault(c => c.Slug == CategorySlug);
            if (ActiveCategory is null) return NotFound();

            query = query.Where(r => r.RulingCategoryId == ActiveCategory.Id);
        }

        // جستجوی درون‌صفحه‌ای روی ستون یکسان‌شده، تا تفاوت نگارش مانع نشود
        if (HasQuery)
        {
            foreach (var term in PersianText.Tokenize(Query))
            {
                var t = term;
                query = query.Where(r => r.SearchText != null && r.SearchText.Contains(t));
            }
        }

        var rulings = await query
            .OrderBy(r => r.RulingCategory.SortOrder)
            .ThenBy(r => r.SortOrder)
            .ThenByDescending(r => r.IsFrequentlyAsked)
            .ToListAsync(ct);

        TotalCount = rulings.Count;
        Groups = rulings.GroupBy(r => r.RulingCategory.Title).ToList();

        return Page();
    }

    /// <summary>
    /// نشانه‌گذاری FAQPage. مهم‌ترین بخش سئوی این صفحه — باعث می‌شود
    /// پرسش و پاسخ‌ها مستقیم زیر نتیجه گوگل نمایش داده شوند.
    /// گوگل حداکثر حدود ده مورد را در نظر می‌گیرد، پس فهرست را محدود می‌کنیم.
    /// </summary>
    public object? BuildFaqSchema()
    {
        var items = Groups.SelectMany(g => g).Take(10).ToList();
        if (items.Count == 0) return null;

        return new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "FAQPage",
            ["mainEntity"] = items.Select(r => new Dictionary<string, object>
            {
                ["@type"] = "Question",
                ["name"] = r.Question,
                ["acceptedAnswer"] = new Dictionary<string, object>
                {
                    ["@type"] = "Answer",
                    ["text"] = StripHtml(r.Answer)
                }
            }).ToList()
        };
    }

    public object BuildBreadcrumbSchema()
    {
        var items = new List<Dictionary<string, object>>
        {
            new() { ["@type"] = "ListItem", ["position"] = 1, ["name"] = "خانه", ["item"] = BaseUrl },
            new() { ["@type"] = "ListItem", ["position"] = 2, ["name"] = "احکام", ["item"] = $"{BaseUrl}/ahkam" }
        };

        if (ActiveCategory is not null)
        {
            items.Add(new Dictionary<string, object>
            {
                ["@type"] = "ListItem",
                ["position"] = 3,
                ["name"] = ActiveCategory.Title,
                ["item"] = $"{BaseUrl}/ahkam?bab={ActiveCategory.Slug}"
            });
        }

        return new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = items
        };
    }

    internal static string StripHtml(string? html) =>
        string.IsNullOrWhiteSpace(html)
            ? string.Empty
            : System.Net.WebUtility.HtmlDecode(
                System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " "))
              .Replace("  ", " ").Trim();
}