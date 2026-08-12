using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Application.Services;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Infrastructure.Services;

/// <summary>
/// تبدیل بین متن تورفته و درخت ذخیره‌شده در دیتابیس.
///
/// هنگام ذخیره، درخت قبلی کامل حذف و از نو ساخته می‌شود. این ساده‌تر و
/// مطمئن‌تر از تطبیق گره به گره است، و چون هر حکم درخت کوچکی دارد،
/// هزینه‌اش ناچیز است.
/// </summary>
public sealed class RulingDiagramService(AppDbContext db) : IRulingDiagramService
{
    public async Task<string> ExportOutlineAsync(int rulingId, CancellationToken ct = default)
    {
        var nodes = await db.RulingNodes.AsNoTracking()
            .Where(n => n.RulingId == rulingId)
            .Include(n => n.Verdicts).ThenInclude(v => v.Marjas).ThenInclude(m => m.Marja)
            .OrderBy(n => n.Depth).ThenBy(n => n.SortOrder)
            .ToListAsync(ct);

        if (nodes.Count == 0) return string.Empty;

        // ساخت درخت در حافظه از فهرست تخت
        var byId = nodes.ToDictionary(n => n.Id, n => new OutlineNode
        {
            Text = n.Text,
            Depth = n.Depth,
            Note = n.Note
        });

        foreach (var n in nodes)
        {
            var outline = byId[n.Id];

            foreach (var v in n.Verdicts.OrderBy(v => v.SortOrder))
            {
                var ov = new OutlineVerdict
                {
                    Text = v.Text,
                    SourceNote = v.SourceNote,
                    IsOthers = v.Scope == VerdictScope.OtherMarjas
                };

                foreach (var link in v.Marjas)
                    ov.MarjaNames.Add(link.Marja.FullName);

                outline.Verdicts.Add(ov);
            }
        }

        var roots = new List<OutlineNode>();
        foreach (var n in nodes)
        {
            if (n.ParentId is null) roots.Add(byId[n.Id]);
            else if (byId.TryGetValue(n.ParentId.Value, out var parent)) parent.Children.Add(byId[n.Id]);
        }

        return DiagramOutline.Compose(roots);
    }

    public async Task<DiagramSaveResult> SaveOutlineAsync(
        int rulingId, string? outline, CancellationToken ct = default)
    {
        var parsed = DiagramOutline.Parse(outline);

        if (parsed.Errors.Count > 0)
            return new DiagramSaveResult(false, 0, 0, parsed.Errors, []);

        var marjas = await db.Marjas.AsNoTracking().ToListAsync(ct);
        var unknown = new List<string>();

        // حذف درخت قبلی — ترتیب مهم است چون کلید خارجی خودارجاع
        // اجازه حذف آبشاری نمی‌دهد
        var existing = await db.RulingNodes
            .Where(n => n.RulingId == rulingId)
            .Include(n => n.Verdicts)
            .OrderByDescending(n => n.Depth)
            .ToListAsync(ct);

        foreach (var node in existing)
        {
            db.RulingVerdicts.RemoveRange(node.Verdicts);
            db.RulingNodes.Remove(node);
        }
        await db.SaveChangesAsync(ct);

        // ساخت درخت تازه
        var order = 0;
        foreach (var root in parsed.Roots)
            await PersistAsync(rulingId, root, null, 0, order++, marjas, unknown, ct);

        await db.SaveChangesAsync(ct);

        return new DiagramSaveResult(
            true, parsed.NodeCount, parsed.VerdictCount, [], unknown.Distinct().ToList());
    }

    private async Task PersistAsync(
        int rulingId, OutlineNode source, int? parentId, int depth, int sortOrder,
        List<Marja> marjas, List<string> unknown, CancellationToken ct)
    {
        var node = new RulingNode
        {
            RulingId = rulingId,
            ParentId = parentId,
            Text = source.Text,
            Note = source.Note,
            Depth = depth,
            SortOrder = sortOrder
        };

        db.RulingNodes.Add(node);
        await db.SaveChangesAsync(ct); // شناسه لازم است تا فرزندان به آن وصل شوند

        var verdictOrder = 0;
        foreach (var ov in source.Verdicts)
        {
            var scope = ov.IsOthers
                ? VerdictScope.OtherMarjas
                : ov.MarjaNames.Count > 0
                    ? VerdictScope.SpecificMarjas
                    : VerdictScope.All;

            var verdict = new RulingVerdict
            {
                RulingNodeId = node.Id,
                Text = ov.Text,
                SourceNote = ov.SourceNote,
                Scope = scope,
                SortOrder = verdictOrder++
            };

            db.RulingVerdicts.Add(verdict);
            await db.SaveChangesAsync(ct);

            foreach (var name in ov.MarjaNames)
            {
                var marja = FindMarja(marjas, name);
                if (marja is null)
                {
                    unknown.Add(name);
                    continue;
                }

                db.RulingVerdictMarjas.Add(new RulingVerdictMarja
                {
                    RulingVerdictId = verdict.Id,
                    MarjaId = marja.Id
                });
            }
        }

        var childOrder = 0;
        foreach (var child in source.Children)
            await PersistAsync(rulingId, child, node.Id, depth + 1, childOrder++, marjas, unknown, ct);
    }

    /// <summary>
    /// تطبیق نام کوتاه با مرجع. در کتاب «سیستانی» نوشته می‌شود، نه
    /// «آیت‌الله العظمی سیستانی»، پس تطبیق جزئی لازم است.
    /// </summary>
    private static Marja? FindMarja(List<Marja> marjas, string name)
    {
        var normalized = PersianText.Normalize(name);
        if (normalized.Length == 0) return null;

        return marjas.FirstOrDefault(m => PersianText.Normalize(m.FullName) == normalized)
            ?? marjas.FirstOrDefault(m => PersianText.Normalize(m.FullName).Contains(normalized, StringComparison.Ordinal))
            ?? marjas.FirstOrDefault(m => PersianText.Normalize(m.Slug) == normalized);
    }
}