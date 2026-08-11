using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Pages.Ahkam;

public class DetailsModel(AppDbContext db) : PageModel
{
    public Ruling Ruling { get; private set; } = default!;
    public IReadOnlyList<Ruling> Related { get; private set; } = [];
    public string BaseUrl { get; private set; } = "";
    public string CanonicalUrl { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var ruling = await db.Rulings.AsNoTracking()
            .Include(r => r.RulingCategory)
            .Include(r => r.Marja)
            .FirstOrDefaultAsync(r => r.Slug == slug && r.Status == PublishStatus.Published, ct);

        if (ruling is null) return NotFound();

        Ruling = ruling;

        var siteSetting = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        BaseUrl = (siteSetting?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        CanonicalUrl = $"{BaseUrl}/ahkam/{ruling.Slug}";

        Related = await db.Rulings.AsNoTracking()
            .Where(r => r.Status == PublishStatus.Published
                     && r.RulingCategoryId == ruling.RulingCategoryId
                     && r.Id != ruling.Id)
            .OrderBy(r => r.SortOrder)
            .Take(5)
            .ToListAsync(ct);

        await db.Rulings
            .Where(r => r.Id == ruling.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.ViewCount, r => r.ViewCount + 1), ct);

        return Page();
    }

    /// <summary>
    /// ???? ???? ?? ???? ??? QAPage ???????? ?? FAQPage ??? —
    /// ??? ?? ???? ??? ?? ???? ???????? ?? ?????? ?? ???????.
    /// </summary>
    public object BuildQaSchema() => new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "QAPage",
        ["mainEntity"] = new Dictionary<string, object?>
        {
            ["@type"] = "Question",
            ["name"] = Ruling.Question,
            ["text"] = Ruling.Question,
            ["answerCount"] = 1,
            ["dateCreated"] = Ruling.CreatedAtUtc.ToString("o"),
            ["acceptedAnswer"] = new Dictionary<string, object?>
            {
                ["@type"] = "Answer",
                ["text"] = IndexModel.StripHtml(Ruling.Answer),
                ["url"] = CanonicalUrl,
                ["dateCreated"] = Ruling.CreatedAtUtc.ToString("o"),
                ["author"] = new Dictionary<string, object?>
                {
                    ["@type"] = Ruling.Marja is null ? "Organization" : "Person",
                    ["name"] = Ruling.Marja?.FullName ?? "????? ?????? ??????????",
                    ["url"] = Ruling.Marja?.OfficialSiteUrl
                }
            }
        }
    };

    public object BuildBreadcrumbSchema() => new Dictionary<string, object>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "BreadcrumbList",
        ["itemListElement"] = new List<Dictionary<string, object>>
        {
            new() { ["@type"] = "ListItem", ["position"] = 1, ["name"] = "????", ["item"] = BaseUrl },
            new() { ["@type"] = "ListItem", ["position"] = 2, ["name"] = "?????", ["item"] = $"{BaseUrl}/ahkam" },
            new()
            {
                ["@type"] = "ListItem", ["position"] = 3,
                ["name"] = Ruling.RulingCategory.Title,
                ["item"] = $"{BaseUrl}/ahkam?bab={Ruling.RulingCategory.Slug}"
            },
            new()
            {
                ["@type"] = "ListItem", ["position"] = 4,
                ["name"] = Ruling.Question, ["item"] = CanonicalUrl
            }
        }
    };
}