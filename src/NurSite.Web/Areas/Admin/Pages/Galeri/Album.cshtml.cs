using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Application.Services;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;
using NurSite.Web.Services;

namespace NurSite.Web.Areas.Admin.Pages.Galeri;

/// <summary>
/// محتوای یک آلبوم: افزودن دسته‌ای و ویرایش تک‌تک اقلام.
/// </summary>
public class AlbumModel(AppDbContext db, ISlugService slugs, FileUploadService uploads) : PageModel
{
    public Album Album { get; private set; } = default!;
    public IReadOnlyList<Photo> Items { get; private set; } = [];

    /// <summary>فایل‌های تصویری که یکجا انتخاب شده‌اند.</summary>
    [BindProperty] public List<IFormFile> Files { get; set; } = [];

    /// <summary>نوع همه فایل‌های همین آپلود. بعداً تک‌تک قابل تغییر است.</summary>
    [BindProperty] public MediaKind UploadKind { get; set; } = MediaKind.Poster;

    [BindProperty] public ItemInput Item { get; set; } = new();
    [BindProperty] public IFormFile? VideoFile { get; set; }

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class ItemInput
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "عنوان را بنویسید")]
        [StringLength(200)]
        [Display(Name = "عنوان")]
        public string Title { get; set; } = default!;

        [Display(Name = "نوع")]
        public MediaKind Kind { get; set; }

        [Required(ErrorMessage = "متن جایگزین تصویر را بنویسید")]
        [StringLength(250)]
        [Display(Name = "متن جایگزین")]
        public string AltText { get; set; } = default!;

        [StringLength(500)]
        [Display(Name = "توضیح")]
        public string? Caption { get; set; }

        [StringLength(600)]
        [Url(ErrorMessage = "نشانی معتبر نیست")]
        [Display(Name = "لینک ویدیو")]
        public string? ExternalVideoUrl { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id, int? edit, CancellationToken ct)
    {
        if (!await LoadAsync(id, ct)) return NotFound();

        var item = edit is null ? null : Items.FirstOrDefault(p => p.Id == edit);
        if (item is not null)
        {
            Item = new ItemInput
            {
                Id = item.Id,
                Title = item.Title,
                Kind = item.Kind,
                AltText = item.AltText,
                Caption = item.Caption,
                ExternalVideoUrl = item.ExternalVideoUrl
            };
        }

        return Page();
    }

    /// <summary>
    /// افزودن دسته‌ای. عنوان و متن جایگزین از نام فایل ساخته می‌شوند تا
    /// آپلود بیست پوستر به بیست بار پر کردن فرم تبدیل نشود؛ بعد تک‌تک
    /// قابل اصلاح‌اند.
    /// </summary>
    public async Task<IActionResult> OnPostUploadAsync(int id, CancellationToken ct)
    {
        if (!await LoadAsync(id, ct)) return NotFound();

        var chosen = Files.Where(f => f.Length > 0).ToList();
        if (chosen.Count == 0)
        {
            Flash = "فایلی انتخاب نشده است.";
            FlashKind = "warn";
            return RedirectToPage(new { id });
        }

        var order = Items.Count == 0 ? 0 : Items.Max(p => p.SortOrder);
        var added = 0;
        var failures = new List<string>();

        foreach (var file in chosen)
        {
            var upload = await uploads.SaveImageAsync(file, "gallery", ct);
            if (!upload.Ok)
            {
                failures.Add($"{file.FileName}: {upload.Error}");
                continue;
            }

            var title = TitleFromFileName(file.FileName);

            db.Photos.Add(new Photo
            {
                AlbumId = id,
                Kind = UploadKind,
                Title = title,
                Slug = await slugs.GenerateUniqueAsync<Photo>(title, null, ct),
                FilePath = upload.Path!,
                AltText = title,
                Width = upload.Width,
                Height = upload.Height,
                FileSizeBytes = upload.SizeBytes,
                SortOrder = ++order
            });

            // هر قلم جدا ذخیره می‌شود چون اسلاگ بعدی باید یکتایی را
            // نسبت به همین قلم هم بسنجد
            await db.SaveChangesAsync(ct);
            added++;
        }

        Flash = failures.Count == 0
            ? $"{added} قلم افزوده شد."
            : $"{added} قلم افزوده شد. {failures.Count} فایل رد شد: {string.Join(" — ", failures.Take(3))}";
        FlashKind = failures.Count == 0 ? "ok" : "warn";

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostSaveItemAsync(int id, CancellationToken ct)
    {
        if (!await LoadAsync(id, ct)) return NotFound();

        var photo = await db.Photos.FirstOrDefaultAsync(p => p.Id == Item.Id && p.AlbumId == id, ct);
        if (photo is null) return NotFound();

        if (VideoFile is not null && VideoFile.Length > 0)
        {
            var upload = await uploads.SaveVideoAsync(VideoFile, "gallery", ct);
            if (!upload.Ok)
                ModelState.AddModelError("VideoFile", upload.Error ?? "آپلود ویدیو ناموفق بود.");
            else
            {
                uploads.Delete(photo.VideoPath);
                photo.VideoPath = upload.Path;
                photo.ExternalVideoUrl = null;
            }
        }

        if (!ModelState.IsValid) return Page();

        if (photo.Title != Item.Title)
            photo.Slug = await slugs.GenerateUniqueAsync<Photo>(Item.Title, photo.Id, ct);

        photo.Title = Item.Title.Trim();
        photo.Kind = Item.Kind;
        photo.AltText = Item.AltText.Trim();
        photo.Caption = Item.Caption?.Trim();

        // لینک بیرونی فقط وقتی معنا دارد که ویدیویی روی سرور خودمان نباشد
        if (!string.IsNullOrWhiteSpace(Item.ExternalVideoUrl))
        {
            uploads.Delete(photo.VideoPath);
            photo.VideoPath = null;
            photo.ExternalVideoUrl = Item.ExternalVideoUrl.Trim();
        }
        else if (VideoFile is null || VideoFile.Length == 0)
        {
            photo.ExternalVideoUrl = null;
        }

        await db.SaveChangesAsync(ct);

        Flash = "تغییرات ذخیره شد.";
        FlashKind = "ok";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteItemAsync(int id, int itemId, CancellationToken ct)
    {
        var photo = await db.Photos.FirstOrDefaultAsync(p => p.Id == itemId && p.AlbumId == id, ct);
        if (photo is null) return NotFound();

        var image = photo.FilePath;
        var video = photo.VideoPath;

        db.Photos.Remove(photo);
        await db.SaveChangesAsync(ct);

        // حذف اینجا فیزیکی است، پس فایل‌ها هم باید بروند وگرنه روی
        // دیسک می‌مانند بدون اینکه چیزی به آن‌ها اشاره کند
        uploads.Delete(image);
        uploads.Delete(video);

        Flash = $"«{photo.Title}» حذف شد.";
        FlashKind = "ok";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostMoveAsync(int id, int itemId, string direction, CancellationToken ct)
    {
        var items = await db.Photos
            .Where(p => p.AlbumId == id)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .ToListAsync(ct);

        var index = items.FindIndex(p => p.Id == itemId);
        var target = direction == "up" ? index - 1 : index + 1;

        if (index < 0 || target < 0 || target >= items.Count)
            return RedirectToPage(new { id });

        (items[index].SortOrder, items[target].SortOrder) =
            (items[target].SortOrder, items[index].SortOrder);

        // اگر هر دو ترتیب یکسان بودند جابه‌جایی اثری ندارد؛ به همه شماره
        // پیاپی می‌دهیم تا از این پس کار کند
        if (items[index].SortOrder == items[target].SortOrder)
        {
            (items[index], items[target]) = (items[target], items[index]);
            for (var i = 0; i < items.Count; i++)
                items[i].SortOrder = i + 1;
        }

        await db.SaveChangesAsync(ct);
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(int id, CancellationToken ct)
    {
        var album = await db.Albums.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (album is null) return false;

        Album = album;
        Items = await db.Photos.AsNoTracking()
            .Where(p => p.AlbumId == id)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .ToListAsync(ct);

        return true;
    }

    /// <summary>
    /// «poster-ashura-01.jpg» را به «poster ashura 01» تبدیل می‌کند.
    /// عنوان موقتی است تا کاربر اصلاحش کند، ولی از «بدون عنوان» بهتر است.
    /// </summary>
    private static string TitleFromFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName).Trim();
        name = name.Replace('_', ' ').Replace('-', ' ');

        while (name.Contains("  ")) name = name.Replace("  ", " ");

        return string.IsNullOrWhiteSpace(name) ? "بدون عنوان" : name;
    }
}