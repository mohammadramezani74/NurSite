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

        // نشانی از سرویس آپلود خودمان می‌آید، ولی چون از دیتابیس خوانده
        // می‌شود بررسی می‌کنیم که از پوشه آپلود بیرون نزند
        if (!item.AudioPath.StartsWith("/uploads/", StringComparison.Ordinal))
            return Results.NotFound();

        var absolute = Path.Combine(
            env.WebRootPath,
            item.AudioPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        var uploadsRoot = Path.Combine(env.WebRootPath, "uploads");
        if (!Path.GetFullPath(absolute).StartsWith(Path.GetFullPath(uploadsRoot), StringComparison.Ordinal))
            return Results.NotFound();

        if (!File.Exists(absolute)) return Results.NotFound();

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