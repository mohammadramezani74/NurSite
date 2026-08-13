using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;
using NurSite.Web.Services;

namespace NurSite.Web.Areas.Admin.Pages.Majmooeha;

/// <summary>
/// مجموعه‌های سخنرانی — مثل «شرح دعای ابوحمزه ثمالی» که چند جلسه دارد.
/// </summary>
public class IndexModel(AppDbContext db, ISlugService slugs, FileUploadService uploads) : PageModel
{
    public sealed record Row(LectureSeries Series, int LectureCount, int PublishedCount);

    public IReadOnlyList<Row> Rows { get; private set; } = [];

    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty] public IFormFile? CoverFile { get; set; }

    public string CanonicalBase { get; private set; } = "";

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "عنوان مجموعه را بنویسید")]
        [StringLength(250)]
        [Display(Name = "عنوان")]
        public string Title { get; set; } = default!;

        [StringLength(250)]
        [Display(Name = "نشانی")]
        public string? Slug { get; set; }

        [StringLength(1000, ErrorMessage = "توضیح نباید بیش از ۱۰۰۰ کاراکتر باشد")]
        [Display(Name = "توضیح")]
        public string? Description { get; set; }

        [Display(Name = "تصویر مجموعه")]
        public string? CoverImagePath { get; set; }

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

        var series = Rows.FirstOrDefault(r => r.Series.Id == edit)?.Series;
        if (series is null) return;

        Input = new InputModel
        {
            Id = series.Id,
            Title = series.Title,
            Slug = series.Slug,
            Description = series.Description,
            CoverImagePath = series.CoverImagePath,
            MetaTitle = series.MetaTitle,
            MetaDescription = series.MetaDescription
        };
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (CoverFile is not null && CoverFile.Length > 0)
        {
            var upload = await uploads.SaveImageAsync(CoverFile, "series", ct);
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
        var series = isNew
            ? new LectureSeries()
            : await db.LectureSeries.FirstOrDefaultAsync(s => s.Id == Input.Id, ct);

        if (series is null) return NotFound();

        var desired = string.IsNullOrWhiteSpace(Input.Slug) ? Input.Title : Input.Slug;
        series.Slug = await slugs.GenerateUniqueAsync<LectureSeries>(
            desired, isNew ? null : series.Id, ct);

        series.Title = Input.Title.Trim();
        series.Description = Input.Description?.Trim();
        series.CoverImagePath = Input.CoverImagePath;

        // اگر پر نشده باشند از عنوان و توضیح ساخته می‌شوند تا صفحه مجموعه بدون متا نماند
        series.MetaTitle = string.IsNullOrWhiteSpace(Input.MetaTitle)
            ? Truncate(series.Title, 70)
            : Input.MetaTitle.Trim();

        series.MetaDescription = string.IsNullOrWhiteSpace(Input.MetaDescription)
            ? Truncate(series.Description ?? series.Title, 170)
            : Input.MetaDescription.Trim();

        series.OgImagePath = series.CoverImagePath;

        if (isNew) db.LectureSeries.Add(series);
        await db.SaveChangesAsync(ct);

        Flash = isNew ? "مجموعه ثبت شد." : "تغییرات ذخیره شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var series = await db.LectureSeries.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (series is null) return NotFound();

        var count = await db.Lectures.CountAsync(l => l.LectureSeriesId == id, ct);
        if (count > 0)
        {
            Flash = $"«{series.Title}» {count} سخنرانی دارد و حذف نمی‌شود. " +
                    "اول سخنرانی‌ها را به مجموعه دیگری ببرید یا از مجموعه خارجشان کنید.";
            FlashKind = "warn";
            return RedirectToPage();
        }

        var cover = series.CoverImagePath;

        db.LectureSeries.Remove(series);
        await db.SaveChangesAsync(ct);

        uploads.Delete(cover);

        Flash = $"«{series.Title}» حذف شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var list = await db.LectureSeries.AsNoTracking()
            .OrderBy(s => s.Title)
            .ToListAsync(ct);

        var counts = await db.Lectures.AsNoTracking()
            .Where(l => l.LectureSeriesId != null)
            .GroupBy(l => l.LectureSeriesId!.Value)
            .Select(g => new
            {
                Id = g.Key,
                Total = g.Count(),
                Published = g.Count(l => l.Status == PublishStatus.Published)
            })
            .ToListAsync(ct);

        Rows = list.Select(s =>
        {
            var c = counts.FirstOrDefault(x => x.Id == s.Id);
            return new Row(s, c?.Total ?? 0, c?.Published ?? 0);
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