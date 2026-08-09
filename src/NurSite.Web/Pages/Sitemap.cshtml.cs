using System.Text;
using System.Xml;
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
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        // نشانی مبنا از تنظیمات سایت خوانده می‌شود، نه از هدر درخواست.
        // پشت پروکسی معکوس یا CDN، مقدار Request.Host ممکن است نشانی
        // داخلی سرور باشد و نقشه سایت با نشانی‌های غلط ساخته شود.
        var siteSetting = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var baseUrl = (siteSetting?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}")
            .TrimEnd('/');

        var sb = new StringBuilder();
        var xmlSettings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            Async = true,
            OmitXmlDeclaration = false
        };

        await using (var writer = XmlWriter.Create(sb, xmlSettings))
        {
            await writer.WriteStartDocumentAsync();
            await writer.WriteStartElementAsync(null, "urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            // صفحه اصلی
            await WriteUrlAsync(writer, baseUrl, DateTime.UtcNow, "daily", "1.0");

            // فهرست مقالات
            await WriteUrlAsync(writer, $"{baseUrl}/maghalat", DateTime.UtcNow, "daily", "0.9");

            // دسته‌بندی‌ها
            var categories = await db.Categories.AsNoTracking()
                .Select(c => c.Slug)
                .ToListAsync(ct);

            foreach (var slug in categories)
                await WriteUrlAsync(writer, $"{baseUrl}/maghalat?dasteh={Uri.EscapeDataString(slug)}",
                    DateTime.UtcNow, "weekly", "0.6");

            // مقالات منتشرشده
            var articles = await db.Articles.AsNoTracking()
                .Where(a => a.Status == PublishStatus.Published)
                .OrderByDescending(a => a.PublishedAtUtc)
                .Select(a => new { a.Slug, a.PublishedAtUtc, a.UpdatedAtUtc })
                .ToListAsync(ct);

            foreach (var a in articles)
            {
                var lastMod = a.UpdatedAtUtc ?? a.PublishedAtUtc ?? DateTime.UtcNow;
                await WriteUrlAsync(writer,
                    $"{baseUrl}/maghalat/{Uri.EscapeDataString(a.Slug)}",
                    lastMod, "monthly", "0.8");
            }

            await writer.WriteEndElementAsync();
            await writer.WriteEndDocumentAsync();
        }

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    private static async Task WriteUrlAsync(
        XmlWriter writer, string location, DateTime lastModified, string changeFreq, string priority)
    {
        await writer.WriteStartElementAsync(null, "url", null);
        await writer.WriteElementStringAsync(null, "loc", null, location);
        await writer.WriteElementStringAsync(null, "lastmod", null, lastModified.ToString("yyyy-MM-dd"));
        await writer.WriteElementStringAsync(null, "changefreq", null, changeFreq);
        await writer.WriteElementStringAsync(null, "priority", null, priority);
        await writer.WriteEndElementAsync();
    }
}