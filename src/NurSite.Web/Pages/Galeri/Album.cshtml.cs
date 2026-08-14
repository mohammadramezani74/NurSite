using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Pages.Galeri;

public class AlbumModel(AppDbContext db) : PageModel
{
    public Album Album { get; private set; } = default!;
    public IReadOnlyList<Photo> Items { get; private set; } = [];

    public string BaseUrl { get; private set; } = "";
    public string CanonicalUrl { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var album = await db.Albums.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Slug == slug && a.Status == PublishStatus.Published, ct);

        if (album is null) return NotFound();

        Album = album;

        Items = await db.Photos.AsNoTracking()
            .Where(p => p.AlbumId == album.Id)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .ToListAsync(ct);

        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        BaseUrl = (settings?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        CanonicalUrl = $"{BaseUrl}/galeri/{album.Slug}";

        return Page();
    }

    /// <summary>
    /// فهرست اقلام به شکل ساختاریافته. برای گوگل، آلبوم یک صفحه با چند
    /// تصویر است و باید بداند هر کدام نشانی مستقل خودش را دارد.
    /// </summary>
    public object BuildSchema() => new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "ImageGallery",
        ["name"] = Album.Title,
        ["description"] = Album.MetaDescription ?? Album.Description,
        ["inLanguage"] = "fa-IR",
        ["url"] = CanonicalUrl,
        ["numberOfItems"] = Items.Count,
        ["associatedMedia"] = Items.Select(p => new Dictionary<string, object?>
        {
            ["@type"] = p.HasVideo ? "VideoObject" : "ImageObject",
            ["name"] = p.Title,
            ["contentUrl"] = $"{BaseUrl}{p.FilePath}",
            ["thumbnailUrl"] = $"{BaseUrl}{p.FilePath}",
            ["url"] = $"{CanonicalUrl}/{p.Slug}",
            ["width"] = p.Width > 0 ? p.Width : null,
            ["height"] = p.Height > 0 ? p.Height : null
        }).ToList()
    };

    public object BuildBreadcrumbSchema() => new Dictionary<string, object>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "BreadcrumbList",
        ["itemListElement"] = new List<Dictionary<string, object>>
        {
            new() { ["@type"] = "ListItem", ["position"] = 1, ["name"] = "خانه", ["item"] = BaseUrl },
            new() { ["@type"] = "ListItem", ["position"] = 2, ["name"] = "گالری", ["item"] = $"{BaseUrl}/galeri" },
            new() { ["@type"] = "ListItem", ["position"] = 3, ["name"] = Album.Title, ["item"] = CanonicalUrl }
        }
    };
}