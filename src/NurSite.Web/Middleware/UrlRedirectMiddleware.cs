using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Middleware;

/// <summary>
/// نشانی‌های قدیمی را با کد ۳۰۱ به نشانی جدید هدایت می‌کند.
/// جدول ریدایرکت‌ها یکجا کش می‌شود تا برای هر درخواست به دیتابیس نرویم؛
/// سایت مذهبی معمولاً چند ده ریدایرکت دارد، نه چند هزار تا.
/// </summary>
public sealed class UrlRedirectMiddleware(RequestDelegate next, ILogger<UrlRedirectMiddleware> logger)
{
    private const string CacheKey = "url:redirects";
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(5);

    public async Task InvokeAsync(HttpContext context, AppDbContext db, IMemoryCache cache)
    {
        var path = context.Request.Path.Value;

        // فایل‌های استاتیک و پنل هرگز ریدایرکت نمی‌شوند
        if (string.IsNullOrEmpty(path) ||
            path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase) ||
            path.Contains('.'))
        {
            await next(context);
            return;
        }

        var map = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheFor;

            return await db.UrlRedirects.AsNoTracking()
                .Where(r => r.IsActive)
                .ToDictionaryAsync(
                    r => r.FromPath.ToLowerInvariant(),
                    r => new RedirectTarget(r.Id, r.ToPath, r.StatusCode));
        });

        var key = path.TrimEnd('/').ToLowerInvariant();
        if (string.IsNullOrEmpty(key)) key = "/";

        if (map is not null && map.TryGetValue(key, out var target))
        {
            // رشته پرس‌وجو حفظ می‌شود تا پارامترهای کمپین از بین نروند
            var destination = target.ToPath + context.Request.QueryString;

            logger.LogInformation("ریدایرکت {From} به {To}", path, target.ToPath);

            // شمارش در پس‌زمینه تا پاسخ کاربر معطل نماند
            _ = IncrementHitAsync(db, target.Id);

            context.Response.Redirect(destination, permanent: target.StatusCode == 301);
            return;
        }

        await next(context);
    }

    private static async Task IncrementHitAsync(AppDbContext db, int id)
    {
        try
        {
            await db.UrlRedirects
                .Where(r => r.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.HitCount, r => r.HitCount + 1));
        }
        catch
        {
            // شمارش بازدید ریدایرکت آنقدر مهم نیست که درخواست را بشکند
        }
    }

    private sealed record RedirectTarget(int Id, string ToPath, int StatusCode);
}

public static class UrlRedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseUrlRedirects(this IApplicationBuilder app) =>
        app.UseMiddleware<UrlRedirectMiddleware>();
}