using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Application.Services;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Entities;
using NurSite.Infrastructure.Identity;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Areas.Admin.Pages.Ahkam;

[Authorize(Policy = Permissions.Rulings.Answer)]
public class NemodarModel(
    AppDbContext db,
    IRulingDiagramService diagrams) : PageModel
{
    public Ruling Ruling { get; private set; } = default!;
    public IReadOnlyList<Marja> Marjas { get; private set; } = [];

    [BindProperty] public string? Outline { get; set; }

    /// <summary>پیش‌نمایش درخت خوانده‌شده، پیش از ذخیره.</summary>
    public OutlineParseResult? Preview { get; private set; }

    public IReadOnlyList<string> UnknownMarjas { get; private set; } = [];

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        if (!await LoadAsync(id, ct)) return NotFound();

        Outline = await diagrams.ExportOutlineAsync(id, ct);
        Preview = DiagramOutline.Parse(Outline);
        return Page();
    }

    /// <summary>بررسی متن بدون ذخیره — برای دیدن نتیجه پیش از اعمال.</summary>
    public async Task<IActionResult> OnPostPreviewAsync(int id, CancellationToken ct)
    {
        if (!await LoadAsync(id, ct)) return NotFound();

        Preview = DiagramOutline.Parse(Outline);
        UnknownMarjas = FindUnknownMarjas(Preview);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(int id, CancellationToken ct)
    {
        if (!await LoadAsync(id, ct)) return NotFound();

        var result = await diagrams.SaveOutlineAsync(id, Outline, ct);

        if (!result.Ok)
        {
            Preview = DiagramOutline.Parse(Outline);
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);
            return Page();
        }

        // وضعیت نموداری بودن حکم را با محتوای واقعی هماهنگ کن
        var ruling = await db.Rulings.FirstAsync(r => r.Id == id, ct);
        ruling.HasDiagram = result.NodeCount > 0;

        // متن جستجو باید با نمودار تازه هماهنگ شود، وگرنه حکم
        // با عبارتی که داخل نمودار است پیدا نمی‌شود
        var nodes = await db.RulingNodes.AsNoTracking()
            .Where(n => n.RulingId == id).Select(n => n.Text).ToListAsync(ct);
        var verdicts = await db.RulingVerdicts.AsNoTracking()
            .Where(v => v.RulingNode.RulingId == id).Select(v => v.Text).ToListAsync(ct);

        ruling.SearchText = PersianText.Normalize(string.Join(' ',
            new[] { ruling.Question, ruling.Question, ruling.Answer, ruling.FatwaNote }
                .Concat(nodes).Concat(verdicts)));

        await db.SaveChangesAsync(ct);

        var message = $"نمودار ذخیره شد: {result.NodeCount} شرط و {result.VerdictCount} حکم.";
        if (result.UnknownMarjas.Count > 0)
        {
            message += " مراجع ناشناخته که ثبت نشدند: " + string.Join("، ", result.UnknownMarjas);
            FlashKind = "warn";
        }
        else
        {
            FlashKind = "ok";
        }

        Flash = message;
        return RedirectToPage("./Nemodar", new { id });
    }

    private async Task<bool> LoadAsync(int id, CancellationToken ct)
    {
        var ruling = await db.Rulings.AsNoTracking()
            .Include(r => r.RulingCategory)
            .Include(r => r.RulingSource)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (ruling is null) return false;
        Ruling = ruling;

        Marjas = await db.Marjas.AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder).ThenBy(m => m.FullName)
            .ToListAsync(ct);

        return true;
    }

    /// <summary>نام‌هایی که در متن آمده ولی مرجعی با آن ثبت نشده است.</summary>
    private List<string> FindUnknownMarjas(OutlineParseResult parsed)
    {
        var names = new List<string>();
        Collect(parsed.Roots, names);

        return names
            .Distinct()
            .Where(n => !Marjas.Any(m =>
                PersianText.Normalize(m.FullName).Contains(PersianText.Normalize(n), StringComparison.Ordinal)))
            .ToList();

        static void Collect(IEnumerable<OutlineNode> nodes, List<string> into)
        {
            foreach (var node in nodes)
            {
                foreach (var verdict in node.Verdicts)
                    into.AddRange(verdict.MarjaNames);

                Collect(node.Children, into);
            }
        }
    }
}