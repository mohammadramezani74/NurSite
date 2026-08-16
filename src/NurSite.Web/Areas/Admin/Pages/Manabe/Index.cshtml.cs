using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Domain.Entities;
using NurSite.Infrastructure.Identity;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Areas.Admin.Pages.Manabe;

/// <summary>
/// منابع احکام — کتاب‌هایی که احکام از آنها وارد شده است.
/// فهرست و فرم در یک صفحه‌اند، مثل ابواب و مراجع.
/// </summary>
[Authorize(Policy = Permissions.Rulings.View)]
public class IndexModel(AppDbContext db, ISlugService slugs) : PageModel
{
    public sealed record Row(RulingSource Source, int RulingCount, int PublishedCount);

    public IReadOnlyList<Row> Rows { get; private set; } = [];

    [BindProperty] public InputModel Input { get; set; } = new();

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "عنوان کتاب را بنویسید")]
        [StringLength(250)]
        [Display(Name = "عنوان")]
        public string Title { get; set; } = default!;

        [StringLength(250)]
        [Display(Name = "نشانی")]
        public string? Slug { get; set; }

        [StringLength(200)]
        [Display(Name = "نویسنده")]
        public string? Author { get; set; }

        [StringLength(200)]
        [Display(Name = "ویراستار")]
        public string? Editor { get; set; }

        [StringLength(200)]
        [Display(Name = "ناشر")]
        public string? Publisher { get; set; }

        [Range(1300, 1500, ErrorMessage = "سال باید بین ۱۳۰۰ تا ۱۵۰۰ باشد")]
        [Display(Name = "سال انتشار")]
        public int? PublishedYear { get; set; }

        [StringLength(20)]
        [Display(Name = "شابک")]
        public string? Isbn { get; set; }

        [StringLength(100)]
        [Display(Name = "نوبت چاپ")]
        public string? Edition { get; set; }

        [StringLength(400)]
        [Url(ErrorMessage = "نشانی معتبر نیست")]
        [Display(Name = "نشانی معرفی کتاب")]
        public string? Url { get; set; }

        [StringLength(1000)]
        [Display(Name = "توضیح")]
        public string? Description { get; set; }

        [StringLength(1000)]
        [Display(Name = "یادداشت اجازه")]
        public string? PermissionNote { get; set; }

        [Display(Name = "فعال")]
        public bool IsActive { get; set; } = true;

        [Range(0, 999)]
        [Display(Name = "ترتیب")]
        public int SortOrder { get; set; }
    }

    public async Task OnGetAsync(int? edit, CancellationToken ct)
    {
        await LoadAsync(ct);

        if (edit is null) return;

        var source = Rows.FirstOrDefault(r => r.Source.Id == edit)?.Source;
        if (source is null) return;

        Input = new InputModel
        {
            Id = source.Id,
            Title = source.Title,
            Slug = source.Slug,
            Author = source.Author,
            Editor = source.Editor,
            Publisher = source.Publisher,
            PublishedYear = source.PublishedYear,
            Isbn = source.Isbn,
            Edition = source.Edition,
            Url = source.Url,
            Description = source.Description,
            PermissionNote = source.PermissionNote,
            IsActive = source.IsActive,
            SortOrder = source.SortOrder
        };
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(ct);
            return Page();
        }

        var isNew = Input.Id == 0;
        var source = isNew
            ? new RulingSource()
            : await db.RulingSources.FirstOrDefaultAsync(s => s.Id == Input.Id, ct);

        if (source is null) return NotFound();

        var desired = string.IsNullOrWhiteSpace(Input.Slug) ? Input.Title : Input.Slug;
        source.Slug = await slugs.GenerateUniqueAsync<RulingSource>(
            desired, isNew ? null : source.Id, ct);

        source.Title = Input.Title.Trim();
        source.Author = Input.Author?.Trim();
        source.Editor = Input.Editor?.Trim();
        source.Publisher = Input.Publisher?.Trim();
        source.PublishedYear = Input.PublishedYear;
        source.Isbn = Input.Isbn?.Trim();
        source.Edition = Input.Edition?.Trim();
        source.Url = string.IsNullOrWhiteSpace(Input.Url) ? null : Input.Url.Trim();
        source.Description = Input.Description?.Trim();
        source.PermissionNote = Input.PermissionNote?.Trim();
        source.IsActive = Input.IsActive;
        source.SortOrder = Input.SortOrder;

        if (isNew) db.RulingSources.Add(source);
        await db.SaveChangesAsync(ct);

        Flash = isNew ? "منبع ثبت شد." : "تغییرات ذخیره شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var source = await db.RulingSources.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (source is null) return NotFound();

        // احکام حذف نمی‌شوند؛ کلید خارجی SetNull است. اما ادمین باید
        // بداند که ارجاع آنها به کتاب از بین می‌رود.
        var count = await db.Rulings.CountAsync(r => r.RulingSourceId == id, ct);
        if (count > 0)
        {
            Flash = $"«{source.Title}» به {count} حکم متصل است و حذف نمی‌شود. " +
                    "برای پنهان کردنش، به‌جای حذف آن را غیرفعال کنید.";
            FlashKind = "warn";
            return RedirectToPage();
        }

        db.RulingSources.Remove(source);
        await db.SaveChangesAsync(ct);

        Flash = $"«{source.Title}» حذف شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var sources = await db.RulingSources.AsNoTracking()
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Title)
            .ToListAsync(ct);

        var counts = await db.Rulings.AsNoTracking()
            .Where(r => r.RulingSourceId != null)
            .GroupBy(r => r.RulingSourceId!.Value)
            .Select(g => new
            {
                Id = g.Key,
                Total = g.Count(),
                Published = g.Count(r => r.Status == Domain.Enums.PublishStatus.Published)
            })
            .ToListAsync(ct);

        Rows = sources.Select(s =>
        {
            var c = counts.FirstOrDefault(x => x.Id == s.Id);
            return new Row(s, c?.Total ?? 0, c?.Published ?? 0);
        }).ToList();

        if (Input.Id == 0 && Input.SortOrder == 0)
            Input.SortOrder = sources.Count == 0 ? 1 : sources.Max(s => s.SortOrder) + 1;
    }
}