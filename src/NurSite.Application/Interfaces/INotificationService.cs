namespace NurSite.Application.Interfaces;

/// <summary>
/// اطلاع‌رسانی به کاربر.
///
/// فعلاً پیاده‌سازی فقط لاگ می‌گیرد و کاربر با کد رهگیری پیگیری می‌کند.
/// برای افزودن پیامک، کافی است پیاده‌سازی تازه‌ای از این قرارداد نوشته
/// و در DependencyInjection جایگزین شود — هیچ صفحه‌ای تغییر نمی‌کند.
/// </summary>
public interface INotificationService
{
    /// <summary>پرسش ثبت شد و کد رهگیری صادر شده است.</summary>
    Task QuestionReceivedAsync(NotificationTarget target, string trackingCode, CancellationToken ct = default);

    /// <summary>پاسخ پرسش آماده شده است.</summary>
    Task AnswerReadyAsync(NotificationTarget target, string trackingCode, CancellationToken ct = default);
}

/// <summary>گیرنده اطلاع‌رسانی. هر کانالی فیلد مربوط به خودش را برمی‌دارد.</summary>
public sealed record NotificationTarget(string? Mobile, string? Email, string? DisplayName)
{
    public bool HasMobile => !string.IsNullOrWhiteSpace(Mobile);
    public bool HasEmail => !string.IsNullOrWhiteSpace(Email);
}