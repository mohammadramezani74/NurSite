using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Domain.Entities;
using NurSite.Infrastructure.Identity;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Areas.Admin.Pages.Dastebandi;

[Authorize(Policy = Permissions.Articles.Edit)]
public class EditModel(AppDbContext db, ISlugService slugs) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public SelectList ParentOptions { get; private set; } = default!;
    public bool IsNew => Input.Id == 0;
    public string CanonicalBase { get; private set; } = "";
    public int ArticleCount { get; private set; }

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "عنوان دسته را بنویسید")]
        [StringLength(150, ErrorMessage = "عنوان نباید بیش از ۱۵۰ کاراکتر باشد")]
        [Display(Name = "عنوان")]
        public string Title { get; set; } = default!;

        [StringLength(150)]
        [Display(Name = "نشانی صفحه")]
        public string? Slug { get; set; }

        [StringLength(500, ErrorMessage = "توضیح نباید بیش از ۵۰۰ کاراکتر باشد")]
        [Display(Name = "توضیح")]
        public string? Description { get; set; }

        [Display(Name = "دسته والد")]
        public int? ParentId { get; set; }

        [Range(0, 999, ErrorMessage = "ترتیب باید عددی بین ۰ تا ۹۹۹ باشد")]
        [Display(Name = "ترتیب نمایش")]
        public int SortOrder { get; set; }

        [StringLength(70, ErrorMessage = "عنوان متا نباید بیش از ۷۰ کاراکتر باشد")]
        [Display(Name = "عنوان متا")]
        public string? MetaTitle { get; set; }

        [StringLength(170, ErrorMessage = "توضیح متا نباید بیش از ۱۷۰ کاراکتر باشد")]
        [Display(Name = "توضیح متا")]
        public string? MetaDescription { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int? id, CancellationToken ct)
    {
        if (id is null or 0)
        {
            await LoadOptionsAsync(null, ct);
            Input.SortOrder = await NextSortOrderAsync(null, ct);
            return Page();
        }

        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null) return NotFound();

        Input = new InputModel
        {
            Id = category.Id,
            Title = category.Title,
            Slug = category.Slug,
            Description = category.Description,
            ParentId = category.ParentId,
            SortOrder = category.SortOrder,
            MetaTitle = category.MetaTitle,
            MetaDescription = category.MetaDescription
        };

        ArticleCount = await db.Articles.CountAsync(a => a.CategoryId == category.Id, ct);
        await LoadOptionsAsync(category.Id, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadOptionsAsync(Input.Id == 0 ? null : Input.Id, ct);

        // دسته نمی‌تواند والد خودش باشد
        if (Input.ParentId is not null && Input.ParentId == Input.Id)
            ModelState.AddModelError("Input.ParentId", "یک دسته نمی‌تواند والد خودش باشد.");

        // و نمی‌تواند زیر یکی از فرزندان خودش برود، وگرنه حلقه ساخته می‌شود
        if (Input.Id != 0 && Input.ParentId is not null &&
            await IsDescendantAsync(Input.ParentId.Value, Input.Id, ct))
        {
            ModelState.AddModelError("Input.ParentId",
                "این دسته زیرمجموعه خودش قرار می‌گیرد و ساختار حلقه می‌شود.");
        }

        if (!ModelState.IsValid) return Page();

        var isNew = Input.Id == 0;
        var category = isNew
            ? new Category()
            : await db.Categories.FirstOrDefaultAsync(c => c.Id == Input.Id, ct);

        if (category is null) return NotFound();

        var desiredSlug = string.IsNullOrWhiteSpace(Input.Slug) ? Input.Title : Input.Slug;
        category.Slug = await slugs.GenerateUniqueAsync<Category>(
            desiredSlug, isNew ? null : category.Id, ct);

        category.Title = Input.Title.Trim();
        category.Description = Input.Description?.Trim();
        category.ParentId = Input.ParentId;
        category.SortOrder = Input.SortOrder;

        // اگر متا خالی باشد از عنوان و توضیح ساخته می‌شود تا صفحه دسته
        // هم بدون متادیتا منتشر نشود
        category.MetaTitle = string.IsNullOrWhiteSpace(Input.MetaTitle)
            ? Truncate($"مقالات {category.Title}", 70)
            : Input.MetaTitle.Trim();

        category.MetaDescription = string.IsNullOrWhiteSpace(Input.MetaDescription)
            ? Truncate(string.IsNullOrWhiteSpace(category.Description)
                ? $"مجموعه مقالات و یادداشت‌های مؤسسه نورالثقلین در موضوع {category.Title}"
                : category.Description, 170)
            : Input.MetaDescription.Trim();

        if (isNew) db.Categories.Add(category);

        await db.SaveChangesAsync(ct);

        Flash = isNew ? "دسته‌بندی ساخته شد." : "تغییرات ذخیره شد.";
        FlashKind = "ok";
        return RedirectToPage("./Index");
    }

    /// <summary>بررسی اینکه آیا candidate در زیرشاخه‌های ancestor هست یا نه.</summary>
    private async Task<bool> IsDescendantAsync(int candidateId, int ancestorId, CancellationToken ct)
    {
        var all = await db.Categories.AsNoTracking()
            .Select(c => new { c.Id, c.ParentId })
            .ToListAsync(ct);

        var current = all.FirstOrDefault(c => c.Id == candidateId);
        var guard = 0;

        while (current?.ParentId is not null && guard++ < 50)
        {
            if (current.ParentId == ancestorId) return true;
            current = all.FirstOrDefault(c => c.Id == current.ParentId);
        }
        return false;
    }

    private async Task LoadOptionsAsync(int? excludeId, CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking()
            .Where(c => excludeId == null || c.Id != excludeId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
            .ToListAsync(ct);

        ParentOptions = new SelectList(categories, nameof(Category.Id), nameof(Category.Title));

        var siteSetting = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        CanonicalBase = (siteSetting?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
    }

    private async Task<int> NextSortOrderAsync(int? parentId, CancellationToken ct)
    {
        var max = await db.Categories
            .Where(c => c.ParentId == parentId)
            .Select(c => (int?)c.SortOrder)
            .MaxAsync(ct);

        return (max ?? 0) + 1;
    }

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