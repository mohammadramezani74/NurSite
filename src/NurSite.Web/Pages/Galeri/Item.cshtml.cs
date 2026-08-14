using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Services;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Pages.Galeri;

/// <summary>
/// صفحه یک قلم گالری. دلیل وجودش سئوست: «پوستر شهادت امام حسین» عبارتی
/// است که جستجو می‌شود، و یک تصویر داخل شبکه بی‌نام شانسی برای پیدا شدن ندارد.
/// </summary>
public class ItemModel(AppDbContext db) : PageModel
{
    public Photo Item { get; private set; } = default!;
    public IReadOnlyList<Photo> Siblings { get; private set; } = [];

    public string BaseUrl { get; private set; } = "";
    public string CanonicalUrl { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(string album, string slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var item = await db.Photos.AsNoTracking()
            .Include(p => p.Album)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Album.Status == PublishStatus.Published, ct);

        if (item is null) return NotFound();

        // اسلاگ قلم یکتاست، پس اگر نام آلبوم در نشانی جا افتاده یا عوض
        // شده باشد باز هم پیدا می‌شود؛ در آن حالت به نشانی درست هدایت
        // می‌کنیم تا یک محتوا دو نشانی نداشته باشد.
        if (!string.Equals(item.Album.Slug, album, StringComparison.Ordinal))
            return RedirectToPagePermanent("/Galeri/Item", new { album = item.Album.Slug, slug = item.Slug });

        Item = item;

        Siblings = await db.Photos.AsNoTracking()
            .Where(p => p.AlbumId == item.AlbumId && p.Id != item.Id)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .Take(8)
            .ToListAsync(ct);

        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        BaseUrl = (settings?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        CanonicalUrl = $"{BaseUrl}/galeri/{item.Album.Slug}/{item.Slug}";

        return Page();
    }

    public string KindLabel => MediaKinds.Label(Item.Kind);
    public string SizeLabel => MediaKinds.FormatSize(Item.FileSizeBytes);

    /// <summary>
    /// دانلود از مسیر خودمان می‌گذرد تا شمرده شود و نام فایل معنادار باشد.
    /// </summary>
    public string DownloadUrl => $"/danlod-tasvir/{Item.Id}";

    public object BuildSchema()
    {
        var image = $"{BaseUrl}{Item.FilePath}";

        if (!Item.HasVideo)
            return new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "ImageObject",
                ["name"] = Item.Title,
                ["description"] = Item.Caption ?? Item.AltText,
                ["contentUrl"] = image,
                ["thumbnailUrl"] = image,
                ["url"] = CanonicalUrl,
                ["width"] = Item.Width > 0 ? Item.Width : null,
                ["height"] = Item.Height > 0 ? Item.Height : null,
                ["inLanguage"] = "fa-IR",
                ["isPartOf"] = new Dictionary<string, object?>
                {
                    ["@type"] = "ImageGallery",
                    ["name"] = Item.Album.Title,
                    ["url"] = $"{BaseUrl}/galeri/{Item.Album.Slug}"
                },
                ["creditText"] = "مؤسسه فرهنگی نورالثقلین",
                // این دو برای گوگل یعنی «آزاد است بردارید»، که هدف همین بخش است
                ["acquireLicensePage"] = CanonicalUrl,
                ["creator"] = new Dictionary<string, object>
                {
                    ["@type"] = "Organization",
                    ["name"] = "مؤسسه فرهنگی نورالثقلین"
                }
            };

        return new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "VideoObject",
            ["name"] = Item.Title,
            ["description"] = Item.Caption ?? Item.AltText,
            // کاور، همان تصویری که در فهرست هم دیده می‌شود
            ["thumbnailUrl"] = image,
            ["contentUrl"] = Item.IsExternalVideo ? Item.ExternalVideoUrl : $"{BaseUrl}{Item.VideoPath}",
            ["url"] = CanonicalUrl,
            ["uploadDate"] = Item.Album.CreatedAtUtc.ToString("o"),
            ["inLanguage"] = "fa-IR",
            ["publisher"] = new Dictionary<string, object>
            {
                ["@type"] = "Organization",
                ["name"] = "مؤسسه فرهنگی نورالثقلین"
            }
        };
    }

    public object BuildBreadcrumbSchema() => new Dictionary<string, object>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "BreadcrumbList",
        ["itemListElement"] = new List<Dictionary<string, object>>
        {
            new() { ["@type"] = "ListItem", ["position"] = 1, ["name"] = "خانه", ["item"] = BaseUrl },
            new() { ["@type"] = "ListItem", ["position"] = 2, ["name"] = "گالری", ["item"] = $"{BaseUrl}/galeri" },
            new()
            {
                ["@type"] = "ListItem", ["position"] = 3,
                ["name"] = Item.Album.Title, ["item"] = $"{BaseUrl}/galeri/{Item.Album.Slug}"
            },
            new() { ["@type"] = "ListItem", ["position"] = 4, ["name"] = Item.Title, ["item"] = CanonicalUrl }
        }
    };
}