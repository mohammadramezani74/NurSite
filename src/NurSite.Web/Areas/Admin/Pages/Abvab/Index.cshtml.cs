using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Domain.Entities;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Areas.Admin.Pages.Abvab;

/// <summary>
/// ابواب احکام. چون تعدادشان کم و ساختارشان ساده است، فهرست و فرم
/// در یک صفحه‌اند تا رفت و برگشت بین صفحات لازم نباشد.
/// </summary>
public class IndexModel(AppDbContext db, ISlugService slugs) : PageModel
{
    public sealed record Row(RulingCategory Category, int RulingCount);

    public IReadOnlyList<Row> Rows { get; private set; } = [];

    [BindProperty] public InputModel Input { get; set; } = new();

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "عنوان باب را بنویسید")]
        [StringLength(150)]
        [Display(Name = "عنوان")]
        public string Title { get; set; } = default!;

        [StringLength(150)]
        [Display(Name = "نشانی")]
        public string? Slug { get; set; }

        [StringLength(500)]
        [Display(Name = "توضیح")]
        public string? Description { get; set; }

        [Range(0, 999)]
        [Display(Name = "ترتیب")]
        public int SortOrder { get; set; }
    }

    public async Task OnGetAsync(int? edit, CancellationToken ct)
    {
        await LoadAsync(ct);

        if (edit is null) return;

        var category = Rows.FirstOrDefault(r => r.Category.Id == edit)?.Category;
        if (category is null) return;

        Input = new InputModel
        {
            Id = category.Id,
            Title = category.Title,
            Slug = category.Slug,
            Description = category.Description,
            SortOrder = category.SortOrder
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
        var category = isNew
            ? new RulingCategory()
            : await db.RulingCategories.FirstOrDefaultAsync(c => c.Id == Input.Id, ct);

        if (category is null) return NotFound();

        var desiredSlug = string.IsNullOrWhiteSpace(Input.Slug) ? Input.Title : Input.Slug;
        category.Slug = await slugs.GenerateUniqueAsync<RulingCategory>(
            desiredSlug, isNew ? null : category.Id, ct);

        category.Title = Input.Title.Trim();
        category.Description = Input.Description?.Trim();
        category.SortOrder = Input.SortOrder;

        category.MetaTitle = $"احکام {category.Title}";
        category.MetaDescription = string.IsNullOrWhiteSpace(category.Description)
            ? $"پرسش و پاسخ‌های شرعی در باب {category.Title} مطابق فتاوای مراجع تقلید"
            : category.Description;

        if (isNew) db.RulingCategories.Add(category);
        await db.SaveChangesAsync(ct);

        Flash = isNew ? "باب ساخته شد." : "تغییرات ذخیره شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var category = await db.RulingCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null) return NotFound();

        var hasRulings = await db.Rulings.AnyAsync(r => r.RulingCategoryId == id, ct);
        if (hasRulings)
        {
            Flash = $"«{category.Title}» حکم دارد و حذف نمی‌شود. اول احکامش را به باب دیگری منتقل کنید.";
            FlashKind = "warn";
            return RedirectToPage();
        }

        db.RulingCategories.Remove(category);
        await db.SaveChangesAsync(ct);

        Flash = $"«{category.Title}» حذف شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var categories = await db.RulingCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
            .ToListAsync(ct);

        var counts = await db.Rulings.AsNoTracking()
            .GroupBy(r => r.RulingCategoryId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, ct);

        Rows = categories
            .Select(c => new Row(c, counts.GetValueOrDefault(c.Id)))
            .ToList();

        if (Input.Id == 0 && Input.SortOrder == 0)
            Input.SortOrder = categories.Count == 0 ? 1 : categories.Max(c => c.SortOrder) + 1;
    }
}