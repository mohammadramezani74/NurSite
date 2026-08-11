using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Pages;

/// <summary>
/// نقشه سایت پویا. فقط محتوای منتشرشده می‌آید — صفحات پیش‌نویس یا بایگانی
/// نباید به گوگل معرفی شوند.
/// </summary>
public class SitemapModel(AppDbContext db) : PageModel
{
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        // نشانی مبنا از تنظیمات سایت خوانده می‌شود، نه از هدر درخواست.
        // پشت پروکسی معکوس یا CDN، مقدار Request.Host ممکن است نشانی
        // داخلی سرور باشد و نقشه سایت با نشانی‌های غلط ساخته شود.
        var siteSetting = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var baseUrl = (siteSetting?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}")
            .TrimEnd('/');

        var urls = new List<XElement>
        {
            Url($"{baseUrl}/", DateTime.UtcNow, "daily", "1.0"),
            Url($"{baseUrl}/maghalat", DateTime.UtcNow, "daily", "0.9"),
            Url($"{baseUrl}/ahkam", DateTime.UtcNow, "daily", "0.9")
        };

        // دسته‌بندی‌ها
        var categorySlugs = await db.Categories.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => c.Slug)
            .ToListAsync(ct);

        urls.AddRange(categorySlugs.Select(slug =>
            Url($"{baseUrl}/maghalat?dasteh={Uri.EscapeDataString(slug)}",
                DateTime.UtcNow, "weekly", "0.6")));

        // مقالات منتشرشده
        var articles = await db.Articles.AsNoTracking()
            .Where(a => a.Status == PublishStatus.Published)
            .OrderByDescending(a => a.PublishedAtUtc)
            .Select(a => new { a.Slug, a.PublishedAtUtc, a.UpdatedAtUtc })
            .ToListAsync(ct);

        urls.AddRange(articles.Select(a =>
            Url($"{baseUrl}/maghalat/{Uri.EscapeDataString(a.Slug)}",
                a.UpdatedAtUtc ?? a.PublishedAtUtc ?? DateTime.UtcNow,
                "monthly", "0.8")));

        // ابواب احکام
        var chapterSlugs = await db.RulingCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => c.Slug)
            .ToListAsync(ct);

        urls.AddRange(chapterSlugs.Select(slug =>
            Url($"{baseUrl}/ahkam?bab={Uri.EscapeDataString(slug)}",
                DateTime.UtcNow, "weekly", "0.7")));

        // احکام منتشرشده — اولویت بالاتر از مقالات، چون بیشتر جستجو می‌شوند
        var rulings = await db.Rulings.AsNoTracking()
            .Where(r => r.Status == PublishStatus.Published)
            .OrderBy(r => r.RulingCategoryId).ThenBy(r => r.SortOrder)
            .Select(r => new { r.Slug, r.CreatedAtUtc, r.UpdatedAtUtc })
            .ToListAsync(ct);

        urls.AddRange(rulings.Select(r =>
            Url($"{baseUrl}/ahkam/{Uri.EscapeDataString(r.Slug)}",
                r.UpdatedAtUtc ?? r.CreatedAtUtc,
                "monthly", "0.8")));

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(Ns + "urlset", urls));

        return Content(document.Declaration + Environment.NewLine + document,
            "application/xml", Encoding.UTF8);
    }

    private static XElement Url(string location, DateTime lastModified, string changeFrequency, string priority) =>
        new(Ns + "url",
            new XElement(Ns + "loc", location),
            new XElement(Ns + "lastmod", lastModified.ToString("yyyy-MM-dd")),
            new XElement(Ns + "changefreq", changeFrequency),
            new XElement(Ns + "priority", priority));
}