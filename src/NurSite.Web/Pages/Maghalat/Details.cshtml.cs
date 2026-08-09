using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Pages.Maghalat;

public class DetailsModel(AppDbContext db) : PageModel
{
    public Article Article { get; private set; } = default!;
    public IReadOnlyList<Article> Related { get; private set; } = [];
    public string BaseUrl { get; private set; } = "";
    public string CanonicalUrl { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var article = await db.Articles.AsNoTracking()
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.Slug == slug && a.Status == PublishStatus.Published, ct);

        if (article is null) return NotFound();

        Article = article;

        // نشانی مبنا از تنظیمات، تا canonical و نشانه‌گذاری ساختاریافته
        // پشت پروکسی هم درست بماند
        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        BaseUrl = (settings?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        CanonicalUrl = $"{BaseUrl}/maghalat/{article.Slug}";

        Related = await db.Articles.AsNoTracking()
            .Where(a => a.Status == PublishStatus.Published
                     && a.CategoryId == article.CategoryId
                     && a.Id != article.Id)
            .OrderByDescending(a => a.PublishedAtUtc)
            .Take(3)
            .ToListAsync(ct);

        // شمارش بازدید خارج از ردیابی EF تا کوئری اضافه نزند
        await db.Articles
            .Where(a => a.Id == article.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.ViewCount, a => a.ViewCount + 1), ct);

        return Page();
    }

    /// <summary>نشانه‌گذاری Article — پایه نمایش غنی مقاله در نتایج گوگل.</summary>
    public object BuildArticleSchema() => new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "Article",
        ["headline"] = Article.Title,
        ["description"] = Article.MetaDescription ?? Article.Summary,
        ["image"] = string.IsNullOrWhiteSpace(Article.CoverImagePath)
            ? null
            : $"{BaseUrl}{Article.CoverImagePath}",
        ["datePublished"] = Article.PublishedAtUtc?.ToString("o"),
        ["dateModified"] = (Article.UpdatedAtUtc ?? Article.PublishedAtUtc)?.ToString("o"),
        ["inLanguage"] = "fa-IR",
        ["wordCount"] = Article.ReadingMinutes * 200,
        ["articleSection"] = Article.Category.Title,
        ["mainEntityOfPage"] = new Dictionary<string, object>
        {
            ["@type"] = "WebPage",
            ["@id"] = CanonicalUrl
        },
        ["author"] = new Dictionary<string, object?>
        {
            ["@type"] = "Person",
            ["name"] = Article.AuthorDisplayName ?? "مؤسسه فرهنگی نورالثقلین"
        },
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

    /// <summary>مسیر راهنما — در نتایج گوگل زیر عنوان نمایش داده می‌شود.</summary>
    public object BuildBreadcrumbSchema() => new Dictionary<string, object>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "BreadcrumbList",
        ["itemListElement"] = new List<Dictionary<string, object>>
        {
            new()
            {
                ["@type"] = "ListItem", ["position"] = 1,
                ["name"] = "خانه", ["item"] = BaseUrl
            },
            new()
            {
                ["@type"] = "ListItem", ["position"] = 2,
                ["name"] = "مقالات", ["item"] = $"{BaseUrl}/maghalat"
            },
            new()
            {
                ["@type"] = "ListItem", ["position"] = 3,
                ["name"] = Article.Category.Title,
                ["item"] = $"{BaseUrl}/maghalat?dasteh={Article.Category.Slug}"
            },
            new()
            {
                ["@type"] = "ListItem", ["position"] = 4,
                ["name"] = Article.Title, ["item"] = CanonicalUrl
            }
        }
    };
}