using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Entities;
using NurSite.Infrastructure.Identity;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Areas.Admin.Pages.Dastebandi;

[Authorize(Policy = Permissions.Articles.Edit)]
public class IndexModel(AppDbContext db) : PageModel
{
    /// <summary>یک دسته‌بندی، همراه با عمقش در درخت و تعداد مقالاتش.</summary>
    public sealed record Node(Category Category, int Depth, int ArticleCount);

    public IReadOnlyList<Node> Nodes { get; private set; } = [];

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
            .ToListAsync(ct);

        var counts = await db.Articles.AsNoTracking()
            .GroupBy(a => a.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, ct);

        Nodes = Flatten(categories, counts, parentId: null, depth: 0).ToList();
    }

    /// <summary>
    /// درخت دسته‌بندی‌ها را به یک فهرست تخت تبدیل می‌کند تا در جدول با
    /// تورفتگی نمایش داده شود. عمق هر گره همان مقدار تورفتگی است.
    /// </summary>
    private static IEnumerable<Node> Flatten(
        List<Category> all, Dictionary<int, int> counts, int? parentId, int depth)
    {
        foreach (var category in all.Where(c => c.ParentId == parentId))
        {
            yield return new Node(category, depth, counts.GetValueOrDefault(category.Id));

            foreach (var child in Flatten(all, counts, category.Id, depth + 1))
                yield return child;
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null) return NotFound();

        // دسته‌ای که مقاله دارد حذف نمی‌شود. کلید خارجی مقاله Restrict است،
        // پس حذفش در پایگاه داده هم شکست می‌خورد؛ اینجا پیش از آن جلویش را
        // می‌گیریم تا کاربر به‌جای خطای دیتابیس، پیام روشن ببیند.
        var hasArticles = await db.Articles.AnyAsync(a => a.CategoryId == id, ct);
        if (hasArticles)
        {
            Flash = $"«{category.Title}» مقاله دارد و حذف نمی‌شود. " +
                    "اول مقاله‌ها را به دسته دیگری ببرید.";
            FlashKind = "warn";
            return RedirectToPage();
        }

        var hasChildren = await db.Categories.AnyAsync(c => c.ParentId == id, ct);
        if (hasChildren)
        {
            Flash = $"«{category.Title}» زیردسته دارد و حذف نمی‌شود.";
            FlashKind = "warn";
            return RedirectToPage();
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);

        Flash = $"«{category.Title}» حذف شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    /// <summary>جابه‌جایی یک دسته بالا و پایین، میان هم‌ردیف‌های خودش.</summary>
    public async Task<IActionResult> OnPostMoveAsync(int id, string direction, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null) return NotFound();

        var siblings = await db.Categories
            .Where(c => c.ParentId == category.ParentId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
            .ToListAsync(ct);

        var index = siblings.FindIndex(c => c.Id == id);
        var target = direction == "up" ? index - 1 : index + 1;

        if (index < 0 || target < 0 || target >= siblings.Count)
            return RedirectToPage();

        (siblings[index].SortOrder, siblings[target].SortOrder) =
            (siblings[target].SortOrder, siblings[index].SortOrder);

        // اگر هر دو ترتیب یکسان بودند، جابه‌جایی هیچ اثری ندارد. در این حالت
        // به همه هم‌ردیف‌ها شماره پیاپی می‌دهیم تا از این پس جابه‌جایی کار کند.
        if (siblings[index].SortOrder == siblings[target].SortOrder)
        {
            (siblings[index], siblings[target]) = (siblings[target], siblings[index]);
            for (var i = 0; i < siblings.Count; i++)
                siblings[i].SortOrder = i + 1;
        }

        await db.SaveChangesAsync(ct);
        return RedirectToPage();
    }
}