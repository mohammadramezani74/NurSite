using Microsoft.EntityFrameworkCore;
using NurSite.Application.DTOs;
using NurSite.Application.Interfaces;
using NurSite.Application.Services;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Infrastructure.Services;

/// <summary>
/// جستجو در مقالات، احکام و سخنرانی‌ها.
///
/// روی ستون یکسان‌شده SearchText انجام می‌شود، نه روی متن اصلی — تا
/// تفاوت‌های نگارشی فارسی مانع پیدا شدن نتیجه نشوند.
///
/// امتیازدهی ساده است: هر واژه‌ای که در عنوان باشد ارزش بیشتری از
/// حضورش در متن دارد، و حکمی که همه واژه‌ها را داشته باشد بالاتر می‌آید.
/// </summary>
public sealed class SearchService(AppDbContext db) : ISearchService
{
    private const int TitleWeight = 10;
    private const int BodyWeight = 3;
    private const int AllTermsBonus = 20;

    public async Task<SearchResponse> SearchAsync(
        string? query,
        SearchKind kind = SearchKind.All,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var terms = PersianText.Tokenize(query);

        if (terms.Length == 0)
            return new SearchResponse(query ?? string.Empty, [], 0, 1, pageSize, []);

        var hits = new List<SearchHit>();

        if (kind is SearchKind.All or SearchKind.Ruling)
            hits.AddRange(await SearchRulingsAsync(terms, ct));

        if (kind is SearchKind.All or SearchKind.Article)
            hits.AddRange(await SearchArticlesAsync(terms, ct));

        if (kind is SearchKind.All or SearchKind.Lecture)
            hits.AddRange(await SearchLecturesAsync(terms, ct));

        var ordered = hits
            .OrderByDescending(h => h.Score)
            .ThenByDescending(h => h.DateUtc)
            .ToList();

        if (page < 1) page = 1;

        var paged = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new SearchResponse(query ?? string.Empty, paged, ordered.Count, page, pageSize, terms);
    }

    public async Task<IReadOnlyList<SearchHit>> SuggestAsync(
        string? query, int take = 6, CancellationToken ct = default)
    {
        var response = await SearchAsync(query, SearchKind.All, 1, take, ct);
        return response.Hits;
    }

    // ---------------------------------------------------------------

    private async Task<List<SearchHit>> SearchRulingsAsync(string[] terms, CancellationToken ct)
    {
        var query = db.Rulings.AsNoTracking()
            .Include(r => r.RulingCategory)
            .Where(r => r.Status == PublishStatus.Published);

        // هر واژه باید جایی در متن باشد — منطق AND، نه OR.
        // با OR نتایج بی‌ربط زیاد می‌شوند.
        foreach (var term in terms)
        {
            var t = term;
            query = query.Where(r => r.SearchText != null && r.SearchText.Contains(t));
        }

        var rows = await query
            .Select(r => new
            {
                r.Question,
                r.Answer,
                r.Slug,
                Category = r.RulingCategory.Title,
                r.CreatedAtUtc,
                r.IsFrequentlyAsked
            })
            .Take(200)
            .ToListAsync(ct);

        return rows.Select(r =>
        {
            var normalizedTitle = PersianText.Normalize(r.Question);
            var normalizedBody = PersianText.Normalize(r.Answer);
            var score = Score(terms, normalizedTitle, normalizedBody);

            // احکام پرتکرار کمی بالاتر می‌آیند چون بیشتر جستجو می‌شوند
            if (r.IsFrequentlyAsked) score += 5;

            return new SearchHit(
                SearchKind.Ruling,
                r.Question,
                Snippet(r.Answer, terms),
                $"/ahkam/{r.Slug}",
                r.Category,
                r.CreatedAtUtc,
                score);
        }).ToList();
    }

    private async Task<List<SearchHit>> SearchArticlesAsync(string[] terms, CancellationToken ct)
    {
        var query = db.Articles.AsNoTracking()
            .Include(a => a.Category)
            .Where(a => a.Status == PublishStatus.Published);

        foreach (var term in terms)
        {
            var t = term;
            query = query.Where(a => a.SearchText != null && a.SearchText.Contains(t));
        }

        var rows = await query
            .Select(a => new
            {
                a.Title,
                a.Summary,
                a.Body,
                a.Slug,
                Category = a.Category.Title,
                a.PublishedAtUtc
            })
            .Take(200)
            .ToListAsync(ct);

        return rows.Select(a =>
        {
            var normalizedTitle = PersianText.Normalize(a.Title);
            var normalizedBody = PersianText.Normalize($"{a.Summary} {a.Body}");
            var score = Score(terms, normalizedTitle, normalizedBody);

            return new SearchHit(
                SearchKind.Article,
                a.Title,
                Snippet(string.IsNullOrWhiteSpace(a.Summary) ? a.Body : a.Summary, terms),
                $"/maghalat/{a.Slug}",
                a.Category,
                a.PublishedAtUtc,
                score);
        }).ToList();
    }

    private async Task<List<SearchHit>> SearchLecturesAsync(string[] terms, CancellationToken ct)
    {
        var query = db.Lectures.AsNoTracking()
            .Include(l => l.Speaker)
            .Include(l => l.LectureSeries)
            .Where(l => l.Status == PublishStatus.Published);

        foreach (var term in terms)
        {
            var t = term;
            query = query.Where(l => l.SearchText != null && l.SearchText.Contains(t));
        }

        var rows = await query
            .Select(l => new
            {
                l.Title,
                l.Description,
                l.Slug,
                Speaker = l.Speaker != null ? l.Speaker.FullName : null,
                Series = l.LectureSeries != null ? l.LectureSeries.Title : null,
                l.PublishedAtUtc
            })
            .Take(200)
            .ToListAsync(ct);

        return rows.Select(l =>
        {
            // نام سخنران کنار عنوان می‌آید چون کاربر معمولاً «فلانی درباره فلان»
            // را جستجو می‌کند و هر دو باید در وزن عنوان حساب شوند
            var normalizedTitle = PersianText.Normalize($"{l.Title} {l.Speaker}");
            var normalizedBody = PersianText.Normalize(l.Description);
            var score = Score(terms, normalizedTitle, normalizedBody);

            return new SearchHit(
                SearchKind.Lecture,
                l.Title,
                Snippet(l.Description, terms),
                $"/sokhanraniha/{l.Slug}",
                l.Series ?? l.Speaker,
                l.PublishedAtUtc,
                score);
        }).ToList();
    }

    private static int Score(string[] terms, string normalizedTitle, string normalizedBody)
    {
        var score = 0;
        var matchedAll = true;

        foreach (var term in terms)
        {
            var inTitle = normalizedTitle.Contains(term, StringComparison.Ordinal);
            var inBody = normalizedBody.Contains(term, StringComparison.Ordinal);

            if (inTitle) score += TitleWeight;
            if (inBody) score += BodyWeight;
            if (!inTitle && !inBody) matchedAll = false;
        }

        if (matchedAll) score += AllTermsBonus;

        // عبارت کامل و پشت‌سرهم، نشانه تطابق دقیق‌تری است
        var phrase = string.Join(' ', terms);
        if (normalizedTitle.Contains(phrase, StringComparison.Ordinal)) score += 25;
        else if (normalizedBody.Contains(phrase, StringComparison.Ordinal)) score += 10;

        return score;
    }

    /// <summary>
    /// بریده‌ای از متن حول اولین واژه پیداشده، تا کاربر ببیند چرا این نتیجه آمده.
    /// </summary>
    private static string Snippet(string? source, string[] terms, int window = 160)
    {
        var plain = PersianText.Normalize(source);
        if (plain.Length == 0) return string.Empty;
        if (plain.Length <= window) return plain;

        var index = -1;
        foreach (var term in terms)
        {
            index = plain.IndexOf(term, StringComparison.Ordinal);
            if (index >= 0) break;
        }

        if (index < 0) return plain[..window].TrimEnd() + "…";

        var start = Math.Max(0, index - window / 3);
        var length = Math.Min(window, plain.Length - start);
        var slice = plain.Substring(start, length);

        // در مرز کلمه ببر
        if (start > 0)
        {
            var firstSpace = slice.IndexOf(' ');
            if (firstSpace > 0 && firstSpace < 20) slice = slice[(firstSpace + 1)..];
        }

        var prefix = start > 0 ? "…" : string.Empty;
        var suffix = start + length < plain.Length ? "…" : string.Empty;

        return prefix + slice.Trim() + suffix;
    }
}