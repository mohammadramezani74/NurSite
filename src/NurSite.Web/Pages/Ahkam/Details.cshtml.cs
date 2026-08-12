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
    public IReadOnlyList<RulingNode> DiagramNodes { get; private set; } = [];
    public string BaseUrl { get; private set; } = "";
    public string CanonicalUrl { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var ruling = await db.Rulings.AsNoTracking()
            .Include(r => r.RulingCategory)
            .Include(r => r.Marja)
            .Include(r => r.RulingSource)
            .FirstOrDefaultAsync(r => r.Slug == slug && r.Status == PublishStatus.Published, ct);

        if (ruling is null) return NotFound();

        Ruling = ruling;

        // درخت نمودار، اگر این حکم نموداری باشد
        if (ruling.HasDiagram)
        {
            var nodes = await db.RulingNodes.AsNoTracking()
                .Where(n => n.RulingId == ruling.Id)
                .Include(n => n.Verdicts).ThenInclude(v => v.Marjas).ThenInclude(m => m.Marja)
                .OrderBy(n => n.Depth).ThenBy(n => n.SortOrder)
                .ToListAsync(ct);

            // بازسازی رابطه والد و فرزند در حافظه، تا رندر بازگشتی ممکن شود
            var byId = nodes.ToDictionary(n => n.Id);
            foreach (var node in nodes)
            {
                if (node.ParentId is not null && byId.TryGetValue(node.ParentId.Value, out var parent))
                    parent.Children.Add(node);
            }

            DiagramNodes = nodes;
        }

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
    /// برای صفحه تک حکم، نوع QAPage مناسب‌تر از FAQPage است —
    /// چون کل صفحه حول یک پرسش می‌چرخد، نه فهرستی از پرسش‌ها.
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
                ["text"] = BuildAnswerText(),
                ["url"] = CanonicalUrl,
                ["dateCreated"] = Ruling.CreatedAtUtc.ToString("o"),
                ["author"] = new Dictionary<string, object?>
                {
                    ["@type"] = Ruling.Marja is null ? "Organization" : "Person",
                    ["name"] = Ruling.Marja?.FullName ?? "مؤسسه فرهنگی نورالثقلین",
                    ["url"] = Ruling.Marja?.OfficialSiteUrl
                }
            }
        }
    };

    /// <summary>
    /// متن پاسخ برای نشانه‌گذاری. در احکام نموداری، درخت به متن خطی
    /// تبدیل می‌شود چون گوگل ساختار درختی را نمی‌فهمد و پاسخ خالی
    /// باعث نادیده گرفتن کل نشانه‌گذاری می‌شود.
    /// </summary>
    private string BuildAnswerText()
    {
        if (!Ruling.HasDiagram || DiagramNodes.Count == 0)
            return IndexModel.StripHtml(Ruling.Answer);

        var sb = new System.Text.StringBuilder();

        if (!string.IsNullOrWhiteSpace(Ruling.Answer))
            sb.Append(IndexModel.StripHtml(Ruling.Answer)).Append(' ');

        void Walk(IEnumerable<RulingNode> nodes)
        {
            foreach (var node in nodes.OrderBy(n => n.SortOrder))
            {
                sb.Append(node.Text);

                foreach (var verdict in node.Verdicts.OrderBy(v => v.SortOrder))
                {
                    sb.Append(' ');
                    if (verdict.Scope == Domain.Enums.VerdictScope.SpecificMarjas)
                        sb.Append(string.Join("، ", verdict.Marjas.Select(m => m.Marja.FullName))).Append(": ");
                    else if (verdict.Scope == Domain.Enums.VerdictScope.OtherMarjas)
                        sb.Append("دیگر مراجع: ");

                    sb.Append(verdict.Text);
                }

                sb.Append(". ");
                Walk(node.Children);
            }
        }

        Walk(DiagramNodes.Where(n => n.ParentId is null));
        return sb.ToString().Trim();
    }

    public object BuildBreadcrumbSchema() => new Dictionary<string, object>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "BreadcrumbList",
        ["itemListElement"] = new List<Dictionary<string, object>>
        {
            new() { ["@type"] = "ListItem", ["position"] = 1, ["name"] = "خانه", ["item"] = BaseUrl },
            new() { ["@type"] = "ListItem", ["position"] = 2, ["name"] = "احکام", ["item"] = $"{BaseUrl}/ahkam" },
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