using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Extensions;

/// <summary>
/// دو مسیر کوچک برای فایل‌های صوتی: دانلود و شمارش پخش.
///
/// چرا صفحه رِیزر نشدند؟ چون هیچ‌کدام HTML برنمی‌گردانند و مسیر دانلود
/// باید بتواند درخواست بازه‌ای (Range) را جواب بدهد تا کاربر بتواند
/// وسط فایل بپرد و دانلود نیمه‌کاره را ادامه بدهد.
/// </summary>
public static class AudioEndpoints
{
    public static void MapAudioEndpoints(this WebApplication app)
    {
        app.MapGet("/danlod/{id:int}", DownloadAsync);
        app.MapPost("/api/pakhsh/{id:int}", CountPlayAsync);
        app.MapGet("/danlod-tasvir/{id:int}", DownloadImageAsync);
    }

    private static async Task<IResult> DownloadAsync(
        int id,
        HttpContext ctx,
        AppDbContext db,
        IWebHostEnvironment env,
        CancellationToken ct)
    {
        var item = await db.Lectures.AsNoTracking()
            .Where(l => l.Id == id && l.Status == PublishStatus.Published)
            .Select(l => new { l.Id, l.Slug, l.AudioPath, l.DownloadAccess })
            .FirstOrDefaultAsync(ct);

        if (item is null || string.IsNullOrWhiteSpace(item.AudioPath))
            return Results.NotFound();

        switch (item.DownloadAccess)
        {
            // نبودنِ دکمه دانلود کافی نیست؛ کسی که نشانی را حدس بزند هم
            // نباید فایل را بگیرد. اینجا ۴۰۴ می‌دهیم نه ۴۰۳، تا وجود
            // نداشتن و اجازه نداشتن از بیرون یکسان دیده شوند.
            case DownloadAccess.Disabled:
                return Results.NotFound();

            case DownloadAccess.SignedIn when ctx.User.Identity?.IsAuthenticated != true:
                return Results.Redirect($"/vorood?returnUrl={Uri.EscapeDataString(ctx.Request.Path)}");
        }

        var absolute = ResolveUploadPath(env, item.AudioPath);
        if (absolute is null) return Results.NotFound();

        await db.Lectures
            .Where(l => l.Id == item.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.DownloadCount, l => l.DownloadCount + 1), ct);

        // نام فایل از اسلاگ ساخته می‌شود تا کاربر در پوشه دانلودش
        // یک مشت شناسه تصادفی نبیند
        return Results.File(
            absolute,
            contentType: "audio/mpeg",
            fileDownloadName: $"{item.Slug}.mp3",
            enableRangeProcessing: true);
    }

    /// <summary>
    /// دانلود یک قلم گالری.
    ///
    /// برخلاف صوت، سیاست دسترسی ندارد: پوستر مناسبتی برای بازنشر ساخته
    /// شده و محدود کردنش با هدفش می‌جنگد. فقط شمرده می‌شود و نام فایل
    /// از اسلاگ ساخته می‌شود تا در پوشه دانلود کاربر معنا داشته باشد.
    /// </summary>
    private static async Task<IResult> DownloadImageAsync(
        int id,
        AppDbContext db,
        IWebHostEnvironment env,
        CancellationToken ct)
    {
        var item = await db.Photos.AsNoTracking()
            .Where(p => p.Id == id && p.Album.Status == PublishStatus.Published)
            .Select(p => new { p.Id, p.Slug, p.FilePath })
            .FirstOrDefaultAsync(ct);

        if (item is null || string.IsNullOrWhiteSpace(item.FilePath))
            return Results.NotFound();

        var resolved = ResolveUploadPath(env, item.FilePath);
        if (resolved is null) return Results.NotFound();

        await db.Photos
            .Where(p => p.Id == item.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.DownloadCount, p => p.DownloadCount + 1), ct);

        var extension = Path.GetExtension(resolved).ToLowerInvariant();
        var contentType = extension switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };

        return Results.File(resolved, contentType, $"{item.Slug}{extension}", enableRangeProcessing: true);
    }

    /// <summary>
    /// نشانی ذخیره‌شده را به مسیر واقعی روی دیسک تبدیل می‌کند و مطمئن
    /// می‌شود از پوشه آپلود بیرون نمی‌زند. مقدار از دیتابیس می‌آید، پس
    /// حتی با اینکه خودمان نوشته‌ایمش، بررسی می‌شود.
    /// </summary>
    private static string? ResolveUploadPath(IWebHostEnvironment env, string webPath)
    {
        if (!webPath.StartsWith("/uploads/", StringComparison.Ordinal)) return null;

        var absolute = Path.Combine(
            env.WebRootPath,
            webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        var uploadsRoot = Path.GetFullPath(Path.Combine(env.WebRootPath, "uploads"));
        if (!Path.GetFullPath(absolute).StartsWith(uploadsRoot, StringComparison.Ordinal)) return null;

        return File.Exists(absolute) ? absolute : null;
    }

    /// <summary>
    /// شمارش پخش. پخش‌کننده بعد از پانزده ثانیه شنیدن این را صدا می‌زند،
    /// نه موقع فشردن دکمه — وگرنه هر کلیک اشتباهی هم یک پخش حساب می‌شد.
    /// </summary>
    private static async Task<IResult> CountPlayAsync(
        int id,
        HttpContext ctx,
        AppDbContext db,
        IMemoryCache cache,
        CancellationToken ct)
    {
        // یک نشانی در بازه کوتاه فقط یک بار شمرده می‌شود، تا رفرش پیاپی
        // یا درخواست دستی عدد را باد نکند
        var fingerprint = $"play:{id}:{ctx.Connection.RemoteIpAddress}";
        if (cache.TryGetValue(fingerprint, out _)) return Results.NoContent();

        var affected = await db.Lectures
            .Where(l => l.Id == id && l.Status == PublishStatus.Published)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.PlayCount, l => l.PlayCount + 1), ct);

        if (affected == 0) return Results.NotFound();

        cache.Set(fingerprint, true, TimeSpan.FromMinutes(30));
        return Results.NoContent();
    }
}