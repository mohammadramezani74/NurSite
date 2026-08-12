using System.Text;
using System.Text.RegularExpressions;

namespace NurSite.Application.Services;

/// <summary>یک گره خوانده‌شده از متن طرح‌نما، پیش از ذخیره در دیتابیس.</summary>
public sealed class OutlineNode
{
    public string Text { get; set; } = "";
    public int Depth { get; set; }
    public string? Note { get; set; }
    public List<OutlineVerdict> Verdicts { get; } = [];
    public List<OutlineNode> Children { get; } = [];
}

/// <summary>حکم یک شاخه، با فهرست نام مراجعی که آن نظر را دارند.</summary>
public sealed class OutlineVerdict
{
    public string Text { get; set; } = "";

    /// <summary>خالی یعنی نظر همه مراجع یکسان است.</summary>
    public List<string> MarjaNames { get; } = [];

    /// <summary>یعنی «دیگر مراجع».</summary>
    public bool IsOthers { get; set; }

    public string? SourceNote { get; set; }
}

public sealed record OutlineParseResult(
    IReadOnlyList<OutlineNode> Roots,
    IReadOnlyList<string> Errors,
    int NodeCount,
    int VerdictCount);

/// <summary>
/// خواندن و نوشتن نمودار شرطی به شکل متن تورفته.
///
/// وارد کردن یک کتاب ۲۲۴ صفحه‌ای با فرم‌های کلیکی عملی نیست. با این قالب،
/// کاربر نمودار را همان‌طور که در کتاب می‌بیند تایپ می‌کند:
///
///   - حیوان به طریق شرعی ذبح شده => پاک است
///   - معلوم نیست ذبح شرعی شده
///     - از مسلمانان خریداری شده
///       - ساخت کشور اسلامی => پاک است
///       - ساخت کشور غیراسلامی
///         => [سیستانی، مکرم] پاک است
///         => [دیگر مراجع] نجس است
///
/// هر تورفتگی دو فاصله یا یک تب است. «=&gt;» حکم را از شرط جدا می‌کند و
/// کروشه فهرست مراجع را مشخص می‌کند. پانویس با «#» بعد از متن می‌آید.
/// </summary>
public static partial class DiagramOutline
{
    private const int SpacesPerLevel = 2;

    [GeneratedRegex(@"^(?<indent>[\s\t]*)(?<marker>[-*•]|=>|<=|=＞)?\s*(?<body>.*)$", RegexOptions.Compiled)]
    private static partial Regex LinePattern();

    [GeneratedRegex(@"^\[(?<marjas>[^\]]+)\]\s*(?<text>.+)$", RegexOptions.Compiled)]
    private static partial Regex VerdictPattern();

    private static readonly string[] OthersKeywords =
        ["دیگر مراجع", "سایر مراجع", "بقیه مراجع", "دیگران", "مابقی"];

    // ---------------------------------------------------------------
    // خواندن
    // ---------------------------------------------------------------

    public static OutlineParseResult Parse(string? outline)
    {
        var roots = new List<OutlineNode>();
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(outline))
            return new OutlineParseResult(roots, errors, 0, 0);

        // پشته گره‌های باز، برای پیدا کردن والد هر سطح
        var stack = new List<OutlineNode>();
        var nodeCount = 0;
        var verdictCount = 0;

        var lines = outline.Replace("\r\n", "\n").Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var match = LinePattern().Match(raw);
            if (!match.Success) continue;

            var indent = match.Groups["indent"].Value;
            var marker = match.Groups["marker"].Value;
            var body = match.Groups["body"].Value.Trim();

            if (body.Length == 0) continue;

            var depth = MeasureDepth(indent);
            var isVerdict = marker is "=>" or "<=" or "=＞";

            // ---------- خط حکم ----------
            if (isVerdict)
            {
                if (stack.Count == 0)
                {
                    errors.Add($"سطر {i + 1}: حکم پیش از آنکه شرطی تعریف شود آمده است.");
                    continue;
                }

                // حکم به نزدیک‌ترین شرطِ کم‌عمق‌تر تعلق می‌گیرد
                var owner = stack.LastOrDefault(n => n.Depth < depth) ?? stack[^1];
                owner.Verdicts.Add(ParseVerdict(body));
                verdictCount++;
                continue;
            }

            // ---------- خط شرط ----------
            var (text, note) = SplitNote(body);

            // حکم می‌تواند در همان خط با => بیاید
            OutlineVerdict? inline = null;
            var arrowAt = text.IndexOf("=>", StringComparison.Ordinal);
            if (arrowAt > 0)
            {
                var verdictPart = text[(arrowAt + 2)..].Trim();
                text = text[..arrowAt].Trim();
                if (verdictPart.Length > 0)
                {
                    inline = ParseVerdict(verdictPart);
                    verdictCount++;
                }
            }

            if (text.Length == 0)
            {
                errors.Add($"سطر {i + 1}: متن شرط خالی است.");
                continue;
            }

            var node = new OutlineNode { Text = text, Depth = depth, Note = note };
            if (inline is not null) node.Verdicts.Add(inline);
            nodeCount++;

            // گره‌های عمیق‌تر یا هم‌عمق از پشته بیرون می‌روند
            while (stack.Count > 0 && stack[^1].Depth >= depth) stack.RemoveAt(stack.Count - 1);

            if (stack.Count == 0)
            {
                node.Depth = 0;
                roots.Add(node);
            }
            else
            {
                var parent = stack[^1];
                node.Depth = parent.Depth + 1;
                parent.Children.Add(node);
            }

            stack.Add(node);
        }

        if (nodeCount == 0 && !string.IsNullOrWhiteSpace(outline))
            errors.Add("هیچ شرطی خوانده نشد. هر سطر باید با خط تیره شروع شود.");

        return new OutlineParseResult(roots, errors, nodeCount, verdictCount);
    }

    private static int MeasureDepth(string indent)
    {
        var spaces = 0;
        foreach (var c in indent)
            spaces += c == '\t' ? SpacesPerLevel : 1;

        return spaces / SpacesPerLevel;
    }

    private static (string Text, string? Note) SplitNote(string body)
    {
        var hashAt = body.IndexOf('#');
        if (hashAt < 0) return (body, null);

        var text = body[..hashAt].Trim();
        var note = body[(hashAt + 1)..].Trim();
        return (text, note.Length > 0 ? note : null);
    }

    private static OutlineVerdict ParseVerdict(string body)
    {
        var (text, note) = SplitNote(body);
        var verdict = new OutlineVerdict { SourceNote = note };

        var match = VerdictPattern().Match(text);
        if (!match.Success)
        {
            verdict.Text = text;
            return verdict;
        }

        verdict.Text = match.Groups["text"].Value.Trim();

        var names = match.Groups["marjas"].Value
            .Split(',', '،', '/', '؛')
            .Select(n => n.Trim())
            .Where(n => n.Length > 0);

        foreach (var name in names)
        {
            if (OthersKeywords.Any(k => name.Contains(k, StringComparison.Ordinal)))
                verdict.IsOthers = true;
            else
                verdict.MarjaNames.Add(name);
        }

        return verdict;
    }

    // ---------------------------------------------------------------
    // نوشتن — برای بازگرداندن درخت ذخیره‌شده به متن قابل ویرایش
    // ---------------------------------------------------------------

    public static string Compose(IEnumerable<OutlineNode> roots)
    {
        var sb = new StringBuilder();
        foreach (var root in roots) Write(sb, root, 0);
        return sb.ToString().TrimEnd();
    }

    private static void Write(StringBuilder sb, OutlineNode node, int depth)
    {
        var pad = new string(' ', depth * SpacesPerLevel);

        sb.Append(pad).Append("- ").Append(node.Text);
        if (!string.IsNullOrWhiteSpace(node.Note)) sb.Append(" # ").Append(node.Note);
        sb.AppendLine();

        foreach (var verdict in node.Verdicts)
        {
            sb.Append(pad).Append("  => ");

            if (verdict.IsOthers || verdict.MarjaNames.Count > 0)
            {
                var names = new List<string>(verdict.MarjaNames);
                if (verdict.IsOthers) names.Add("دیگر مراجع");
                sb.Append('[').Append(string.Join("، ", names)).Append("] ");
            }

            sb.Append(verdict.Text);
            if (!string.IsNullOrWhiteSpace(verdict.SourceNote)) sb.Append(" # ").Append(verdict.SourceNote);
            sb.AppendLine();
        }

        foreach (var child in node.Children) Write(sb, child, depth + 1);
    }
}