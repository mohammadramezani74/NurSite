using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Entities;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Areas.Admin.Pages.Dastebandi;

public class IndexModel(AppDbContext db) : PageModel
{
    /// <summary>???? ???????? ??? ?? ???? ? ???? ???????.</summary>
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
    /// ???? ?? ?? ????? ??? ????? ?????? ?? ?? ???? ?? ??????? ????? ???? ???.
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

        // ??????? ?? ????? ???? ??? ???????? ????? ?????? ??????? ????????.
        // ???? ????? ?? Restrict ??? ? ?? ??? ??????? ????? ?? ????????
        // ??? ????? ???? ???????? ?? ????? ???????.
        var hasArticles = await db.Articles.AnyAsync(a => a.CategoryId == id, ct);
        if (hasArticles)
        {
            Flash = $"«{category.Title}» ????? ???? ? ??? ???????. ??? ??????? ?? ?? ???? ????? ????? ????.";
            FlashKind = "warn";
            return RedirectToPage();
        }

        var hasChildren = await db.Categories.AnyAsync(c => c.ParentId == id, ct);
        if (hasChildren)
        {
            Flash = $"«{category.Title}» ????????? ???? ? ??? ???????.";
            FlashKind = "warn";
            return RedirectToPage();
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);

        Flash = $"«{category.Title}» ??? ??.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    /// <summary>????????? ?? ??? ???? ?? ????? ??? ?????????.</summary>
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

        // ??? ???????? ??? ??? ?????? ????????? ???? ?????? ?? ???????? ???????
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