using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NurSite.Application.Interfaces;

namespace NurSite.Infrastructure.Services;

public sealed class SmsOptions
{
    /// <summary>کلید وب‌سرویس از بخش برنامه‌نویسان پنل sms.ir.</summary>
    public string? ApiKey { get; set; }

    /// <summary>شناسه قالب اعتبارسنجی که در پنل ساخته شده است.</summary>
    public int VerifyTemplateId { get; set; }

    /// <summary>نام پارامتر کد در همان قالب، بدون علامت #.</summary>
    public string CodeParameterName { get; set; } = "CODE";

    /// <summary>خط ارسال. برای کد یک‌بارمصرف لازم نیست؛ برای ارسال گروهی آینده نگه داشته شده.</summary>
    public string? LineNumber { get; set; }

    /// <summary>
    /// به‌جای ارسال واقعی، کد در لاگ نوشته شود. برای توسعه، تا اعتبار
    /// پنل بی‌جهت خرج نشود.
    /// </summary>
    public bool UseFakeSender { get; set; }
}

/// <summary>
/// ارسال پیامک از راه sms.ir.
///
/// از REST مستقیم استفاده می‌کنیم نه بسته رسمی: تنها متدی که لازم داریم
/// یکی است، و این‌طور مهلت زمانی، لاگ و ترجمه خطاها کاملاً دست خودمان است.
/// </summary>
public sealed class SmsIrSender(
    HttpClient http,
    IOptions<SmsOptions> options,
    ILogger<SmsIrSender> logger) : ISmsSender
{
    private readonly SmsOptions _opt = options.Value;

    public async Task<SmsResult> SendVerificationCodeAsync(string mobile, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opt.ApiKey) || _opt.VerifyTemplateId == 0)
        {
            logger.LogError("پیامک ارسال نشد: کلید وب‌سرویس یا شناسه قالب تنظیم نشده است.");
            return SmsResult.Failure("سرویس پیامک پیکربندی نشده است.");
        }

        var payload = new VerifyRequest(
            // سرویس شماره را بدون صفر ابتدایی می‌خواهد
            Mobile: mobile.TrimStart('0'),
            TemplateId: _opt.VerifyTemplateId,
            Parameters: [new VerifyParameter(_opt.CodeParameterName, code)]);

        try
        {
            using var response = await http.PostAsJsonAsync("v1/send/verify", payload, ct);
            var body = await response.Content.ReadFromJsonAsync<VerifyResponse>(cancellationToken: ct);

            // وضعیت ۱ یعنی موفق. بقیه کدها معنای مشخص دارند و در جدول
            // مستندات آمده‌اند؛ آنچه به کار کاربر می‌آید ترجمه شده است.
            if (response.IsSuccessStatusCode && body?.Status == 1)
                return SmsResult.Success(body.Data?.MessageId);

            var status = body?.Status ?? (int)response.StatusCode;
            logger.LogError("ارسال پیامک ناموفق. وضعیت {Status}، پیام {Message}", status, body?.Message);

            return SmsResult.Failure(Translate(status));
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogError("ارسال پیامک به {Mobile} از مهلت گذشت.", Mask(mobile));
            return SmsResult.Failure("سرویس پیامک پاسخ نداد. چند لحظه دیگر دوباره تلاش کنید.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطای غیرمنتظره در ارسال پیامک به {Mobile}", Mask(mobile));
            return SmsResult.Failure("ارسال پیامک ممکن نشد. چند لحظه دیگر دوباره تلاش کنید.");
        }
    }

    /// <summary>
    /// کد وضعیت سرویس به پیامی که می‌شود به کاربر نشان داد.
    /// خطاهایی که تقصیر ماست (کلید، اعتبار، قالب) پیام عمومی می‌گیرند —
    /// کاربر نباید بداند اعتبار پنل تمام شده است.
    /// </summary>
    private static string Translate(int status) => status switch
    {
        20 => "تعداد درخواست‌ها زیاد بوده است. کمی صبر کنید.",
        104 => "شماره موبایل نادرست است.",
        _ => "ارسال پیامک ممکن نشد. چند لحظه دیگر دوباره تلاش کنید."
    };

    /// <summary>شماره در لاگ کامل ننویسیم.</summary>
    private static string Mask(string mobile) =>
        mobile.Length < 7 ? "***" : $"{mobile[..4]}***{mobile[^2..]}";

    private sealed record VerifyRequest(
        [property: JsonPropertyName("mobile")] string Mobile,
        [property: JsonPropertyName("templateId")] int TemplateId,
        [property: JsonPropertyName("parameters")] VerifyParameter[] Parameters);

    private sealed record VerifyParameter(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("value")] string Value);

    private sealed record VerifyResponse(
        [property: JsonPropertyName("status")] int Status,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("data")] VerifyData? Data);

    private sealed record VerifyData(
        [property: JsonPropertyName("messageId")] long MessageId,
        [property: JsonPropertyName("cost")] decimal Cost);
}

/// <summary>
/// جای سرویس واقعی در محیط توسعه. کد را در لاگ می‌نویسد تا بدون خرج
/// اعتبار و بدون نیاز به سیم‌کارت، کل جریان ورود قابل آزمودن باشد.
/// </summary>
public sealed class FakeSmsSender(ILogger<FakeSmsSender> logger) : ISmsSender
{
    public Task<SmsResult> SendVerificationCodeAsync(string mobile, string code, CancellationToken ct = default)
    {
        logger.LogWarning("پیامک ساختگی → {Mobile} کد {Code}", mobile, code);
        return Task.FromResult(SmsResult.Success());
    }
}