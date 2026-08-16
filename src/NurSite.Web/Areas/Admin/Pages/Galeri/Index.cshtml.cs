using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Web.Services;

using NurSite.Infrastructure.Identity;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Areas.Admin.Pages.Galeri;

/// <summary>
/// فهرست آلبوم‌های گالری. ساخت آلبوم تازه در همین صفحه است؛
/// افزودن پوستر و کلیپ در صفحه خود آلبوم.
/// </summary>
[Authorize(Policy = Permissions.Media.Manage)]
public class IndexModel(AppDbContext db, ISlugService slugs, FileUploadService uploads) : PageModel
{
    public sealed record Row(Album Album, int ItemCount, string? FirstImage);

    public IReadOnlyList<Row> Rows { get; private set; } = [];

    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty] public IFormFile? CoverFile { get; set; }

    public string CanonicalBase { get; private set; } = "";

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "عنوان آلبوم را بنویسید")]
        [StringLength(200, ErrorMessage = "عنوان نباید بیش از ۲۰۰ کاراکتر باشد")]
        [Display(Name = "عنوان")]
        public string Title { get; set; } = default!;

        [StringLength(200)]
        [Display(Name = "نشانی")]
        public string? Slug { get; set; }

        [StringLength(1000, ErrorMessage = "توضیح نباید بیش از ۱۰۰۰ کاراکتر باشد")]
        [Display(Name = "توضیح")]
        public string? Description { get; set; }

        [Display(Name = "تصویر آلبوم")]
        public string? CoverImagePath { get; set; }

        [Display(Name = "وضعیت")]
        public PublishStatus Status { get; set; } = PublishStatus.Draft;

        [StringLength(70, ErrorMessage = "عنوان متا نباید بیش از ۷۰ کاراکتر باشد")]
        [Display(Name = "عنوان متا")]
        public string? MetaTitle { get; set; }

        [StringLength(170, ErrorMessage = "توضیح متا نباید بیش از ۱۷۰ کاراکتر باشد")]
        [Display(Name = "توضیح متا")]
        public string? MetaDescription { get; set; }
    }

    public async Task OnGetAsync(int? edit, CancellationToken ct)
    {
        await LoadAsync(ct);

        if (edit is null) return;

        var album = Rows.FirstOrDefault(r => r.Album.Id == edit)?.Album;
        if (album is null) return;

        Input = new InputModel
        {
            Id = album.Id,
            Title = album.Title,
            Slug = album.Slug,
            Description = album.Description,
            CoverImagePath = album.CoverImagePath,
            Status = album.Status,
            MetaTitle = album.MetaTitle,
            MetaDescription = album.MetaDescription
        };
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (CoverFile is not null && CoverFile.Length > 0)
        {
            var upload = await uploads.SaveImageAsync(CoverFile, "gallery", ct);
            if (!upload.Ok)
                ModelState.AddModelError("CoverFile", upload.Error ?? "آپلود تصویر ناموفق بود.");
            else
                Input.CoverImagePath = upload.Path;
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(ct);
            return Page();
        }

        var isNew = Input.Id == 0;
        var album = isNew
            ? new Album()
            : await db.Albums.FirstOrDefaultAsync(a => a.Id == Input.Id, ct);

        if (album is null) return NotFound();

        var desired = string.IsNullOrWhiteSpace(Input.Slug) ? Input.Title : Input.Slug;
        album.Slug = await slugs.GenerateUniqueAsync<Album>(desired, isNew ? null : album.Id, ct);

        album.Title = Input.Title.Trim();
        album.Description = Input.Description?.Trim();
        album.CoverImagePath = Input.CoverImagePath;
        album.Status = Input.Status;

        album.MetaTitle = string.IsNullOrWhiteSpace(Input.MetaTitle)
            ? Truncate(album.Title, 70)
            : Input.MetaTitle.Trim();

        album.MetaDescription = string.IsNullOrWhiteSpace(Input.MetaDescription)
            ? Truncate(album.Description ?? album.Title, 170)
            : Input.MetaDescription.Trim();

        album.OgImagePath = album.CoverImagePath;

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (isNew)
        {
            album.CreatedById = userId;
            db.Albums.Add(album);
        }
        else
        {
            album.UpdatedById = userId;
        }

        await db.SaveChangesAsync(ct);

        if (isNew)
        {
            // آلبوم تازه بدون محتوا بی‌معناست؛ کاربر را می‌بریم همان‌جا
            // که پوسترها را اضافه کند
            Flash = "آلبوم ساخته شد. حالا پوسترها را اضافه کنید.";
            FlashKind = "ok";
            return RedirectToPage("./Album", new { id = album.Id });
        }

        Flash = "تغییرات ذخیره شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var album = await db.Albums
            .Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (album is null) return NotFound();

        // آلبومی که محتوا دارد یک‌باره حذف نمی‌شود. کلید خارجی Cascade است،
        // یعنی حذف آلبوم همه پوسترهایش را هم می‌برد — بی‌سروصدا و بی‌بازگشت.
        if (album.Photos.Count > 0)
        {
            Flash = $"«{album.Title}» {album.Photos.Count} قلم دارد و حذف نمی‌شود. " +
                    "اول محتوایش را پاک کنید یا آلبوم را پیش‌نویس کنید تا از سایت پنهان شود.";
            FlashKind = "warn";
            return RedirectToPage();
        }

        var cover = album.CoverImagePath;

        db.Albums.Remove(album);
        await db.SaveChangesAsync(ct);

        uploads.Delete(cover);

        Flash = $"«{album.Title}» حذف شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var albums = await db.Albums.AsNoTracking()
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(ct);

        var summaries = await db.Photos.AsNoTracking()
            .GroupBy(p => p.AlbumId)
            .Select(g => new
            {
                AlbumId = g.Key,
                Count = g.Count(),
                // اگر آلبوم کاور نداشته باشد، اولین قلمش را نشان می‌دهیم
                First = g.OrderBy(p => p.SortOrder).Select(p => p.FilePath).FirstOrDefault()
            })
            .ToListAsync(ct);

        Rows = albums.Select(a =>
        {
            var s = summaries.FirstOrDefault(x => x.AlbumId == a.Id);
            return new Row(a, s?.Count ?? 0, a.CoverImagePath ?? s?.First);
        }).ToList();

        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        CanonicalBase = (settings?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
    }

    /// <summary>بریدن متن با رعایت دقیق سقف؛ سه‌نقطه هم یک کاراکتر است.</summary>
    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        value = value.Trim();
        if (value.Length <= max) return value;

        var cut = value[..(max - 1)].TrimEnd();
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > max / 2) cut = cut[..lastSpace];

        return cut + "…";
    }
}