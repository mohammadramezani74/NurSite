using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Services;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Pages.Galeri;

/// <summary>
/// صفحه اصلی گالری: آلبوم‌های منتشرشده، با امکان فیلتر بر اساس نوع.
/// </summary>
public class IndexModel(AppDbContext db) : PageModel
{
    public sealed record AlbumCard(Album Album, int ItemCount, Photo? Preview);

    public IReadOnlyList<AlbumCard> Albums { get; private set; } = [];

    /// <summary>وقتی روی نوعی فیلتر شده، به‌جای آلبوم‌ها خود اقلام نشان داده می‌شوند.</summary>
    public IReadOnlyList<Photo> Items { get; private set; } = [];

    [BindProperty(SupportsGet = true, Name = "no")] public string? KindSlug { get; set; }
    public MediaKind? Kind { get; private set; }

    public string BaseUrl { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        BaseUrl = (settings?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');

        if (!string.IsNullOrWhiteSpace(KindSlug))
        {
            Kind = MediaKinds.FromSectionSlug(KindSlug);
            if (Kind is null) return NotFound();

            // فیلتر نوع، مرز آلبوم را می‌شکند: کسی که دنبال استوری است
            // کاری ندارد کدام مناسبت بوده
            Items = await db.Photos.AsNoTracking()
                .Include(p => p.Album)
                .Where(p => p.Kind == Kind && p.Album.Status == PublishStatus.Published)
                .OrderByDescending(p => p.Album.CreatedAtUtc)
                .ThenBy(p => p.SortOrder)
                .Take(120)
                .ToListAsync(ct);

            return Page();
        }

        var albums = await db.Albums.AsNoTracking()
            .Where(a => a.Status == PublishStatus.Published)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(ct);

        var albumIds = albums.Select(a => a.Id).ToList();

        var photos = await db.Photos.AsNoTracking()
            .Where(p => albumIds.Contains(p.AlbumId))
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);

        Albums = albums
            .Select(a =>
            {
                var own = photos.Where(p => p.AlbumId == a.Id).ToList();
                return new AlbumCard(a, own.Count, own.FirstOrDefault());
            })
            // آلبوم خالی برای بازدیدکننده چیزی ندارد
            .Where(c => c.ItemCount > 0)
            .ToList();

        return Page();
    }

    public string PageTitle => Kind is null
        ? "گالری"
        : MediaKinds.PluralLabel(Kind.Value);

    public string PageDescription => Kind is null
        ? "پوستر، استوری، والپیپر و کلیپ‌های مناسبتی مؤسسه فرهنگی نورالثقلین، برای دانلود و بازنشر."
        : $"دانلود {MediaKinds.PluralLabel(Kind.Value)} مناسبتی مؤسسه فرهنگی نورالثقلین.";

    /// <summary>نشانی تصویر بندانگشتی هر آلبوم — کاور، وگرنه اولین قلمش.</summary>
    public static string? Thumbnail(AlbumCard card) =>
        string.IsNullOrWhiteSpace(card.Album.CoverImagePath)
            ? card.Preview?.FilePath
            : card.Album.CoverImagePath;
}