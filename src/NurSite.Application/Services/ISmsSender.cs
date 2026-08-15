namespace NurSite.Application.Interfaces;

/// <summary>نتیجه ارسال پیامک. پیام خطا فارسی و قابل نمایش به کاربر است.</summary>
public sealed record SmsResult(bool Ok, string? Error = null, long? MessageId = null)
{
    public static SmsResult Success(long? messageId = null) => new(true, null, messageId);
    public static SmsResult Failure(string error) => new(false, error);
}

public interface ISmsSender
{
    /// <summary>
    /// ارسال کد یک‌بارمصرف با قالب اعتبارسنجی.
    /// شماره به شکل ۰۹xxxxxxxxx داده می‌شود؛ تبدیلش به قالب مورد نیاز
    /// سرویس، کار خود پیاده‌سازی است.
    /// </summary>
    Task<SmsResult> SendVerificationCodeAsync(string mobile, string code, CancellationToken ct = default);
}