using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Domain.Entities;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Areas.Admin.Pages.Maraje;

public class IndexModel(AppDbContext db, ISlugService slugs) : PageModel
{
    public sealed record Row(Marja Marja, int RulingCount);

    public IReadOnlyList<Row> Rows { get; private set; } = [];

    [BindProperty] public InputModel Input { get; set; } = new();

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام مرجع را بنویسید")]
        [StringLength(150)]
        [Display(Name = "نام")]
        public string FullName { get; set; } = default!;

        [StringLength(150)]
        [Display(Name = "نشانی")]
        public string? Slug { get; set; }

        [StringLength(300)]
        [Url(ErrorMessage = "نشانی سایت معتبر نیست")]
        [Display(Name = "سایت رسمی")]
        public string? OfficialSiteUrl { get; set; }

        [Range(0, 999)]
        [Display(Name = "ترتیب")]
        public int SortOrder { get; set; }

        [Display(Name = "فعال")]
        public bool IsActive { get; set; } = true;
    }

    public async Task OnGetAsync(int? edit, CancellationToken ct)
    {
        await LoadAsync(ct);

        if (edit is null) return;

        var marja = Rows.FirstOrDefault(r => r.Marja.Id == edit)?.Marja;
        if (marja is null) return;

        Input = new InputModel
        {
            Id = marja.Id,
            FullName = marja.FullName,
            Slug = marja.Slug,
            OfficialSiteUrl = marja.OfficialSiteUrl,
            SortOrder = marja.SortOrder,
            IsActive = marja.IsActive
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
        var marja = isNew
            ? new Marja()
            : await db.Marjas.FirstOrDefaultAsync(m => m.Id == Input.Id, ct);

        if (marja is null) return NotFound();

        var desiredSlug = string.IsNullOrWhiteSpace(Input.Slug) ? Input.FullName : Input.Slug;
        marja.Slug = await slugs.GenerateUniqueAsync<Marja>(
            desiredSlug, isNew ? null : marja.Id, ct);

        marja.FullName = Input.FullName.Trim();
        marja.OfficialSiteUrl = string.IsNullOrWhiteSpace(Input.OfficialSiteUrl)
            ? null
            : Input.OfficialSiteUrl.Trim();
        marja.SortOrder = Input.SortOrder;
        marja.IsActive = Input.IsActive;

        if (isNew) db.Marjas.Add(marja);
        await db.SaveChangesAsync(ct);

        Flash = isNew ? "مرجع اضافه شد." : "تغییرات ذخیره شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var marja = await db.Marjas.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (marja is null) return NotFound();

        // کلید خارجی احکام روی مرجع SetNull است، پس حذف مرجع خودِ احکام را
        // از بین نمی‌برد ولی استنادشان را می‌اندازد و حکمی بی‌مرجع می‌ماند.
        var count = await db.Rulings.CountAsync(r => r.MarjaId == id, ct);
        if (count > 0)
        {
            Flash = $"«{marja.FullName}» به {count} حکم متصل است و حذف نمی‌شود. " +
                    "برای پنهان کردنش، به‌جای حذف آن را غیرفعال کنید.";
            FlashKind = "warn";
            return RedirectToPage();
        }

        db.Marjas.Remove(marja);
        await db.SaveChangesAsync(ct);

        Flash = $"«{marja.FullName}» حذف شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var marjas = await db.Marjas.AsNoTracking()
            .OrderBy(m => m.SortOrder).ThenBy(m => m.FullName)
            .ToListAsync(ct);

        var counts = await db.Rulings.AsNoTracking()
            .Where(r => r.MarjaId != null)
            .GroupBy(r => r.MarjaId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, ct);

        Rows = marjas.Select(m => new Row(m, counts.GetValueOrDefault(m.Id))).ToList();

        // مرجع تازه به انتهای فهرست می‌رود، نه ابتدای آن
        if (Input.Id == 0 && Input.SortOrder == 0)
            Input.SortOrder = marjas.Count == 0 ? 1 : marjas.Max(m => m.SortOrder) + 1;
    }
}