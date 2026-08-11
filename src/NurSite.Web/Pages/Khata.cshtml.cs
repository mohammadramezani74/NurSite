using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Pages;

/// <summary>
/// صفحه خطا. هم برای کدهای وضعیت (۴۰۴، ۴۰۳ و ...) استفاده می‌شود
/// هم برای استثناهای پیش‌بینی‌نشده.
/// </summary>
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class KhataModel(AppDbContext db, ISearchService search, ILogger<KhataModel> logger) : PageModel
{
    public int StatusCode { get; private set; } = 500;
    public string Title { get; private set; } = "خطایی رخ داد";
    public string Message { get; private set; } = "";

    /// <summary>نشانی‌ای که کاربر دنبالش بوده — برای پیشنهاد محتوای مرتبط.</summary>
    public string? AttemptedPath { get; private set; }

    /// <summary>شناسه پیگیری خطا، تا کاربر بتواند گزارشش کند.</summary>
    public string? ErrorId { get; private set; }

    public IReadOnlyList<Article> SuggestedArticles { get; private set; } = [];
    public IReadOnlyList<Ruling> SuggestedRulings { get; private set; } = [];

    public async Task OnGetAsync(int? code, CancellationToken ct)
    {
        StatusCode = code ?? 500;
        Response.StatusCode = StatusCode;

        var statusFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        AttemptedPath = statusFeature?.OriginalPath;

        (Title, Message) = StatusCode switch
        {
            400 => ("درخواست نامعتبر", "اطلاعاتی که فرستاده شد قابل پردازش نبود."),
            403 => ("دسترسی ندارید", "برای دیدن این صفحه مجوز لازم را ندارید."),
            404 => ("صفحه پیدا نشد", "این نشانی وجود ندارد یا جابه‌جا شده است."),
            408 => ("زمان درخواست تمام شد", "پاسخ به موقع نرسید. دوباره تلاش کنید."),
            429 => ("درخواست بیش از حد", "در مدت کوتاهی درخواست زیادی فرستاده شده. کمی صبر کنید."),
            500 => ("خطایی رخ داد", "مشکلی در سرور پیش آمد. در حال بررسی هستیم."),
            503 => ("سایت در دسترس نیست", "سایت موقتاً در حال به‌روزرسانی است."),
            _ => ("خطایی رخ داد", "مشکلی پیش آمد. اگر ادامه داشت با ما تماس بگیرید.")
        };

        if (StatusCode >= 500)
        {
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            ErrorId = HttpContext.TraceIdentifier;

            logger.LogError(exceptionFeature?.Error,
                "خطای {Status} در مسیر {Path} — شناسه {ErrorId}",
                StatusCode, exceptionFeature?.Path ?? AttemptedPath, ErrorId);
        }

        // برای ۴۰۴ محتوای مرتبط پیشنهاد می‌دهیم تا کاربر بن‌بست نخورد
        if (StatusCode == 404)
            await LoadSuggestionsAsync(ct);
    }

    /// <summary>
    /// از روی نشانی اشتباه، محتوای مشابه پیدا می‌کند.
    /// مثلاً اگر کسی /maghalat/حسد-قدیمی را باز کند، مقاله «حسد» پیشنهاد می‌شود.
    /// </summary>
    private async Task LoadSuggestionsAsync(CancellationToken ct)
    {
        try
        {
            var terms = ExtractTerms(AttemptedPath);

            if (!string.IsNullOrWhiteSpace(terms))
            {
                var hits = await search.SuggestAsync(terms, take: 4, ct);
                if (hits.Count > 0)
                {
                    // نتایج جستجو را به موجودیت تبدیل نمی‌کنیم؛ همان لینک‌ها کافی است
                    SuggestedLinks = hits
                        .Select(h => (h.Title, h.Url, h.Kind == Application.DTOs.SearchKind.Ruling))
                        .ToList();
                    return;
                }
            }

            // اگر از نشانی چیزی درنیامد، تازه‌ترین محتوا را نشان می‌دهیم
            SuggestedArticles = await db.Articles.AsNoTracking()
                .Where(a => a.Status == PublishStatus.Published)
                .OrderByDescending(a => a.PublishedAtUtc)
                .Take(3)
                .ToListAsync(ct);

            SuggestedRulings = await db.Rulings.AsNoTracking()
                .Where(r => r.Status == PublishStatus.Published && r.IsFrequentlyAsked)
                .OrderBy(r => r.SortOrder)
                .Take(3)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            // صفحه خطا هرگز نباید خودش خطا بدهد
            logger.LogWarning(ex, "بارگذاری پیشنهادها در صفحه ۴۰۴ ناموفق بود.");
        }
    }

    public List<(string Title, string Url, bool IsRuling)> SuggestedLinks { get; private set; } = [];

    /// <summary>واژه‌های معنادار را از نشانی بیرون می‌کشد.</summary>
    private static string? ExtractTerms(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        var lastSegment = path.TrimEnd('/').Split('/').LastOrDefault();
        if (string.IsNullOrWhiteSpace(lastSegment)) return null;

        var decoded = Uri.UnescapeDataString(lastSegment).Replace('-', ' ');
        return decoded.Length < 3 ? null : decoded;
    }
}