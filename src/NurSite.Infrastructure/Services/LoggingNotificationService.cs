using Microsoft.Extensions.Logging;
using NurSite.Application.Interfaces;

namespace NurSite.Infrastructure.Services;

/// <summary>
/// پیاده‌سازی موقت اطلاع‌رسانی: چیزی ارسال نمی‌کند و فقط لاگ می‌گیرد.
///
/// وقتی پنل پیامکی آماده شد، کلاسی مثل SmsNotificationService بنویسید که
/// همین قرارداد را پیاده کند و در DependencyInjection جایگزینش کنید.
/// </summary>
public sealed class LoggingNotificationService(ILogger<LoggingNotificationService> logger)
    : INotificationService
{
    public Task QuestionReceivedAsync(NotificationTarget target, string trackingCode, CancellationToken ct = default)
    {
        logger.LogInformation(
            "پرسش تازه ثبت شد. کد رهگیری {Code} — گیرنده {Mobile}. اطلاع‌رسانی هنوز فعال نیست.",
            trackingCode, Mask(target.Mobile));

        return Task.CompletedTask;
    }

    public Task AnswerReadyAsync(NotificationTarget target, string trackingCode, CancellationToken ct = default)
    {
        logger.LogInformation(
            "پاسخ پرسش {Code} آماده شد — گیرنده {Mobile}. اطلاع‌رسانی هنوز فعال نیست.",
            trackingCode, Mask(target.Mobile));

        return Task.CompletedTask;
    }

    /// <summary>شماره کامل در لاگ ننشیند.</summary>
    private static string Mask(string? mobile) =>
        string.IsNullOrWhiteSpace(mobile) || mobile.Length < 7
            ? "—"
            : $"{mobile[..4]}***{mobile[^2..]}";
}