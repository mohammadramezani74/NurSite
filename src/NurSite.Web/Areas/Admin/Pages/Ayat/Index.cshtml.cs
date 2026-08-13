using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Entities;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Areas.Admin.Pages.Ayat;

/// <summary>آیات اسلایدر صفحه اصلی.</summary>
public class IndexModel(AppDbContext db) : PageModel
{
    public IReadOnlyList<HeroVerse> Verses { get; private set; } = [];
    public int ActiveCount { get; private set; }

    [BindProperty] public InputModel Input { get; set; } = new();

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "متن عربی آیه را بنویسید")]
        [StringLength(500)]
        [Display(Name = "متن عربی")]
        public string ArabicText { get; set; } = default!;

        [Required(ErrorMessage = "ترجمه فارسی را بنویسید")]
        [StringLength(500)]
        [Display(Name = "ترجمه فارسی")]
        public string PersianText { get; set; } = default!;

        [Required(ErrorMessage = "منبع آیه را بنویسید")]
        [StringLength(150)]
        [Display(Name = "منبع")]
        public string Reference { get; set; } = default!;

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

        var verse = Verses.FirstOrDefault(v => v.Id == edit);
        if (verse is null) return;

        Input = new InputModel
        {
            Id = verse.Id,
            ArabicText = verse.ArabicText,
            PersianText = verse.PersianText,
            Reference = verse.Reference,
            SortOrder = verse.SortOrder,
            IsActive = verse.IsActive
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
        var verse = isNew
            ? new HeroVerse()
            : await db.HeroVerses.FirstOrDefaultAsync(v => v.Id == Input.Id, ct);

        if (verse is null) return NotFound();

        verse.ArabicText = Input.ArabicText.Trim();
        verse.PersianText = Input.PersianText.Trim();
        verse.Reference = Input.Reference.Trim();
        verse.SortOrder = Input.SortOrder;
        verse.IsActive = Input.IsActive;

        if (isNew) db.HeroVerses.Add(verse);
        await db.SaveChangesAsync(ct);

        Flash = isNew ? "آیه اضافه شد." : "تغییرات ذخیره شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, CancellationToken ct)
    {
        var verse = await db.HeroVerses.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (verse is null) return NotFound();

        // اسلایدر باید دست‌کم یک آیه فعال داشته باشد، وگرنه بخش
        // بالای صفحه اصلی خالی می‌ماند
        if (verse.IsActive)
        {
            var others = await db.HeroVerses.CountAsync(v => v.IsActive && v.Id != id, ct);
            if (others == 0)
            {
                Flash = "دست‌کم یک آیه باید فعال بماند، وگرنه اسلایدر صفحه اصلی خالی می‌شود.";
                FlashKind = "warn";
                return RedirectToPage();
            }
        }

        verse.IsActive = !verse.IsActive;
        await db.SaveChangesAsync(ct);

        Flash = verse.IsActive ? "آیه فعال شد." : "آیه غیرفعال شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMoveAsync(int id, string direction, CancellationToken ct)
    {
        var all = await db.HeroVerses
            .OrderBy(v => v.SortOrder).ThenBy(v => v.Id)
            .ToListAsync(ct);

        var index = all.FindIndex(v => v.Id == id);
        var target = direction == "up" ? index - 1 : index + 1;

        if (index < 0 || target < 0 || target >= all.Count) return RedirectToPage();

        (all[index], all[target]) = (all[target], all[index]);
        for (var i = 0; i < all.Count; i++) all[i].SortOrder = i + 1;

        await db.SaveChangesAsync(ct);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var verse = await db.HeroVerses.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (verse is null) return NotFound();

        var remaining = await db.HeroVerses.CountAsync(v => v.Id != id, ct);
        if (remaining == 0)
        {
            Flash = "آخرین آیه حذف نمی‌شود. دست‌کم یکی باید بماند.";
            FlashKind = "warn";
            return RedirectToPage();
        }

        db.HeroVerses.Remove(verse);
        await db.SaveChangesAsync(ct);

        Flash = "آیه حذف شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Verses = await db.HeroVerses.AsNoTracking()
            .OrderBy(v => v.SortOrder).ThenBy(v => v.Id)
            .ToListAsync(ct);

        ActiveCount = Verses.Count(v => v.IsActive);

        if (Input.Id == 0 && Input.SortOrder == 0)
            Input.SortOrder = Verses.Count == 0 ? 1 : Verses.Max(v => v.SortOrder) + 1;
    }
}