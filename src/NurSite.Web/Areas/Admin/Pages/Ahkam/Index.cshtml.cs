using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Areas.Admin.Pages.Ahkam;

public class IndexModel(AppDbContext db) : PageModel
{
    private const int PageSize = 20;

    public IReadOnlyList<Ruling> Rulings { get; private set; } = [];
    public IReadOnlyList<RulingCategory> Categories { get; private set; } = [];
    public IReadOnlyList<Marja> Marjas { get; private set; } = [];

    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true)] public PublishStatus? Status { get; set; }
    [BindProperty(SupportsGet = true)] public int? CategoryId { get; set; }
    [BindProperty(SupportsGet = true)] public int? MarjaId { get; set; }
    [BindProperty(SupportsGet = true)] public bool? Faq { get; set; }
    [BindProperty(SupportsGet = true)] public bool? Diagram { get; set; }
    // نام این پارامتر عمداً «page» نیست. در Razor Pages نام page رزرو شده
    // و خودِ مسیر صفحه را نگه می‌دارد؛ اگر به عنوان route value بفرستیمش،
    // ساخت نشانی به هم می‌ریزد و لینک صفحه‌بندی و اکشن فرم‌ها از کار می‌افتند.
    [BindProperty(SupportsGet = true, Name = "safhe")] public int PageNumber { get; set; } = 1;

    public int TotalCount { get; private set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Categories = await db.RulingCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Title).ToListAsync(ct);

        Marjas = await db.Marjas.AsNoTracking()
            .OrderBy(m => m.SortOrder).ThenBy(m => m.FullName).ToListAsync(ct);

        var query = db.Rulings.AsNoTracking()
            .Include(r => r.RulingCategory)
            .Include(r => r.Marja)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = Q.Trim();
            // جستجو در متن پرسش و پاسخ — کاربر معمولاً عبارتی از دل پاسخ یادش است
            query = query.Where(r => r.Question.Contains(term) || r.Answer.Contains(term));
        }

        if (Status is not null) query = query.Where(r => r.Status == Status);
        if (CategoryId is not null) query = query.Where(r => r.RulingCategoryId == CategoryId);
        if (MarjaId is not null) query = query.Where(r => r.MarjaId == MarjaId);
        if (Faq == true) query = query.Where(r => r.IsFrequentlyAsked);
        if (Diagram == true) query = query.Where(r => r.HasDiagram);

        TotalCount = await query.CountAsync(ct);

        if (PageNumber < 1) PageNumber = 1;
        if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

        Rulings = await query
            .OrderBy(r => r.RulingCategoryId).ThenBy(r => r.SortOrder)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, CancellationToken ct)
    {
        var ruling = await db.Rulings.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (ruling is null) return NotFound();

        ruling.Status = ruling.Status == PublishStatus.Published
            ? PublishStatus.Draft
            : PublishStatus.Published;

        await db.SaveChangesAsync(ct);

        Flash = ruling.Status == PublishStatus.Published ? "حکم منتشر شد." : "حکم به پیش‌نویس برگشت.";
        FlashKind = "ok";
        return RedirectToPage(new { Q, Status, CategoryId, MarjaId, Faq, Diagram, safhe = PageNumber });
    }

    /// <summary>نشان دادن یا برداشتن از بخش «احکام پرتکرار» صفحه اصلی.</summary>
    public async Task<IActionResult> OnPostFaqAsync(int id, CancellationToken ct)
    {
        var ruling = await db.Rulings.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (ruling is null) return NotFound();

        ruling.IsFrequentlyAsked = !ruling.IsFrequentlyAsked;
        await db.SaveChangesAsync(ct);

        // ظرفیت صفحه اصلی چهار تاست ولی ستاره پنجم به بعد هدر نمی‌رود؛
        // هر بازدید چهارتای تصادفی از میان همه ستاره‌دارها می‌آید
        var faqCount = await db.Rulings
            .CountAsync(r => r.IsFrequentlyAsked && r.Status == PublishStatus.Published, ct);

        Flash = ruling.IsFrequentlyAsked
            ? faqCount > 4
                ? $"به احکام پرتکرار اضافه شد. الان {faqCount} حکم پرتکرار دارید و صفحه اصلی هر بار چهارتای تصادفی را نشان می‌دهد."
                : "به احکام پرتکرار صفحه اصلی اضافه شد."
            : "از احکام پرتکرار برداشته شد.";
        FlashKind = "ok";
        return RedirectToPage(new { Q, Status, CategoryId, MarjaId, Faq, Diagram, safhe = PageNumber });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var ruling = await db.Rulings.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (ruling is null) return NotFound();

        db.Rulings.Remove(ruling); // حذف نرم است
        await db.SaveChangesAsync(ct);

        Flash = "حکم حذف شد.";
        FlashKind = "ok";
        return RedirectToPage(new { Q, Status, CategoryId, MarjaId, Faq, Diagram, safhe = PageNumber });
    }
}