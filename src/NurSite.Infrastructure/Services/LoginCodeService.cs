using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NurSite.Application.Interfaces;
using NurSite.Domain.Entities;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Infrastructure.Services;

public sealed class LoginCodeOptions
{
    public int Digits { get; set; } = 5;
    public int LifetimeSeconds { get; set; } = 180;

    /// <summary>فاصله لازم میان دو درخواست کد برای یک شماره.</summary>
    public int ResendCooldownSeconds { get; set; } = 60;

    /// <summary>سقف درخواست کد برای یک شماره در یک ساعت.</summary>
    public int MaxPerHourPerMobile { get; set; } = 5;

    /// <summary>سقف درخواست کد از یک نشانی در یک ساعت.</summary>
    public int MaxPerHourPerIp { get; set; } = 15;

    /// <summary>چند بار وارد کردن کد اشتباه، کد را می‌سوزاند.</summary>
    public int MaxAttempts { get; set; } = 5;
}

public sealed record CodeRequestResult(
    bool Ok,
    string? Error = null,
    int RetryAfterSeconds = 0);

public enum CodeCheckStatus { Ok, NotFound, Expired, Wrong, TooManyAttempts }

public interface ILoginCodeService
{
    Task<CodeRequestResult> RequestAsync(string mobile, string? ip, CancellationToken ct = default);
    Task<CodeCheckStatus> VerifyAsync(string mobile, string code, CancellationToken ct = default);
    Task<int> SecondsUntilResendAsync(string mobile, CancellationToken ct = default);
}

/// <summary>
/// صدور و بررسی کد یک‌بارمصرف.
///
/// سه لایه محدودیت دارد: فاصله میان دو ارسال، سقف ساعتی برای هر شماره،
/// و سقف ساعتی برای هر نشانی. اولی جلوی کلیک پیاپی کاربر را می‌گیرد،
/// دومی جلوی آزار یک شماره را، و سومی جلوی کسی که با شماره‌های تصادفی
/// اعتبار پنل را خالی می‌کند.
/// </summary>
public sealed class LoginCodeService(
    AppDbContext db,
    ISmsSender sms,
    IOptions<LoginCodeOptions> options,
    ILogger<LoginCodeService> logger) : ILoginCodeService
{
    private readonly LoginCodeOptions _opt = options.Value;

    public async Task<CodeRequestResult> RequestAsync(string mobile, string? ip, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var hourAgo = now.AddHours(-1);

        var last = await db.LoginCodes.AsNoTracking()
            .Where(c => c.Mobile == mobile)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (last is not null)
        {
            var elapsed = (now - last.CreatedAtUtc).TotalSeconds;
            if (elapsed < _opt.ResendCooldownSeconds)
                return new CodeRequestResult(false,
                    "کد قبلی هنوز معتبر است.",
                    (int)Math.Ceiling(_opt.ResendCooldownSeconds - elapsed));
        }

        var perMobile = await db.LoginCodes.CountAsync(
            c => c.Mobile == mobile && c.CreatedAtUtc > hourAgo, ct);

        if (perMobile >= _opt.MaxPerHourPerMobile)
        {
            logger.LogWarning("سقف ساعتی کد برای {Mobile} پر شد", Mask(mobile));
            return new CodeRequestResult(false,
                "تعداد درخواست‌های شما زیاد بوده است. یک ساعت دیگر دوباره تلاش کنید.");
        }

        var ipHash = Hash(ip);
        if (ipHash is not null)
        {
            var perIp = await db.LoginCodes.CountAsync(
                c => c.IpHash == ipHash && c.CreatedAtUtc > hourAgo, ct);

            if (perIp >= _opt.MaxPerHourPerIp)
            {
                logger.LogWarning("سقف ساعتی کد برای یک نشانی پر شد");
                return new CodeRequestResult(false,
                    "تعداد درخواست‌ها زیاد بوده است. کمی بعد دوباره تلاش کنید.");
            }
        }

        var code = GenerateCode();

        // پیامک پیش از ذخیره فرستاده می‌شود؛ اگر ارسال شکست بخورد،
        // نباید کدی در دیتابیس بماند که کاربر هرگز دریافتش نکرده
        var sent = await sms.SendVerificationCodeAsync(mobile, code, ct);
        if (!sent.Ok)
            return new CodeRequestResult(false, sent.Error ?? "ارسال پیامک ممکن نشد.");

        db.LoginCodes.Add(new LoginCode
        {
            Mobile = mobile,
            CodeHash = Hash(code)!,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddSeconds(_opt.LifetimeSeconds),
            IpHash = ipHash
        });

        await db.SaveChangesAsync(ct);

        return new CodeRequestResult(true, null, _opt.ResendCooldownSeconds);
    }

    public async Task<CodeCheckStatus> VerifyAsync(string mobile, string code, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var entry = await db.LoginCodes
            .Where(c => c.Mobile == mobile && c.UsedAtUtc == null)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (entry is null) return CodeCheckStatus.NotFound;
        if (entry.FailedAttempts >= _opt.MaxAttempts) return CodeCheckStatus.TooManyAttempts;
        if (now >= entry.ExpiresAtUtc) return CodeCheckStatus.Expired;

        var given = Hash(NormalizeDigits(code))!;

        // مقایسه زمان‌ثابت، تا از روی مدت پاسخ نشود کد را حدس زد
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(given),
                Encoding.UTF8.GetBytes(entry.CodeHash)))
        {
            entry.FailedAttempts++;
            await db.SaveChangesAsync(ct);

            return entry.FailedAttempts >= _opt.MaxAttempts
                ? CodeCheckStatus.TooManyAttempts
                : CodeCheckStatus.Wrong;
        }

        entry.UsedAtUtc = now;
        await db.SaveChangesAsync(ct);

        return CodeCheckStatus.Ok;
    }

    public async Task<int> SecondsUntilResendAsync(string mobile, CancellationToken ct = default)
    {
        var last = await db.LoginCodes.AsNoTracking()
            .Where(c => c.Mobile == mobile)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => (DateTime?)c.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (last is null) return 0;

        var remaining = _opt.ResendCooldownSeconds - (DateTime.UtcNow - last.Value).TotalSeconds;
        return remaining <= 0 ? 0 : (int)Math.Ceiling(remaining);
    }

    /// <summary>
    /// کد تصادفی با مولد امن. Random معمولی برای چیزی که نگهبان حساب
    /// کاربر است کافی نیست.
    /// </summary>
    private string GenerateCode()
    {
        var max = (int)Math.Pow(10, _opt.Digits);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString(new string('0', _opt.Digits));
    }

    /// <summary>کاربر ممکن است کد را با ارقام فارسی وارد کند.</summary>
    private static string NormalizeDigits(string input) =>
        new(input.Trim().Select(c =>
            c is >= '\u06F0' and <= '\u06F9' ? (char)(c - '\u06F0' + '0') :
            c is >= '\u0660' and <= '\u0669' ? (char)(c - '\u0660' + '0') :
            c).ToArray());

    private static string? Hash(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static string Mask(string mobile) =>
        mobile.Length < 7 ? "***" : $"{mobile[..4]}***{mobile[^2..]}";
}