using NurSite.Domain.Enums;

namespace NurSite.Application.Services;

/// <summary>
/// برچسب‌ها و نشانی هر نوع صوت، یک جا.
///
/// چرا یک جا؟ چون نوع صوت هم در پنل نمایش داده می‌شود، هم در نشانی
/// صفحه عمومی، هم در نتیجه جستجو، هم در نقشه سایت. اگر هر کدام برای
/// خودش رشته بنویسند، روزی که نوع تازه‌ای اضافه شود یکی‌شان جا می‌ماند.
/// </summary>
public static class AudioKinds
{
    public static readonly IReadOnlyList<AudioKind> All =
        [AudioKind.Lecture, AudioKind.Madahi, AudioKind.Anthem];

    /// <summary>نام مفرد. مثال: مداحی</summary>
    public static string Label(AudioKind kind) => kind switch
    {
        AudioKind.Madahi => "مداحی",
        AudioKind.Anthem => "سرود مذهبی",
        _ => "سخنرانی"
    };

    /// <summary>نام بخش برای عنوان صفحه. مثال: مداحی‌ها</summary>
    public static string PluralLabel(AudioKind kind) => kind switch
    {
        AudioKind.Madahi => "مداحی‌ها",
        AudioKind.Anthem => "سرودها و آهنگ‌های مذهبی",
        _ => "سخنرانی‌ها"
    };

    /// <summary>گوینده این نوع چه نامیده می‌شود. مثال: مداح</summary>
    public static string SpeakerLabel(AudioKind kind) => kind switch
    {
        AudioKind.Madahi => "مداح",
        AudioKind.Anthem => "خواننده",
        _ => "سخنران"
    };

    /// <summary>بخش نشانی. مثال: madahiha</summary>
    public static string SectionSlug(AudioKind kind) => kind switch
    {
        AudioKind.Madahi => "madahiha",
        AudioKind.Anthem => "sorudha",
        _ => "sokhanraniha"
    };

    /// <summary>نشانی کامل یک صوت در سایت.</summary>
    public static string Url(AudioKind kind, string slug) => $"/{SectionSlug(kind)}/{slug}";

    /// <summary>از روی بخش نشانی، نوع را برمی‌گرداند. برای مسیریابی صفحه عمومی.</summary>
    public static AudioKind? FromSectionSlug(string? section) => section?.ToLowerInvariant() switch
    {
        "sokhanraniha" => AudioKind.Lecture,
        "madahiha" => AudioKind.Madahi,
        "sorudha" => AudioKind.Anthem,
        _ => null
    };

    /// <summary>مدت به شکل خوانا. مثال: ۱:۰۵:۳۰ یا ۴:۱۲</summary>
    public static string FormatDuration(int seconds)
    {
        if (seconds <= 0) return "—";

        var span = TimeSpan.FromSeconds(seconds);
        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{span.Minutes}:{span.Seconds:00}";
    }

    /// <summary>
    /// خواندن مدت از ورودی کاربر به شکل «۴:۱۲» یا «۱:۰۵:۳۰» یا فقط ثانیه.
    /// ارقام فارسی هم پذیرفته می‌شوند.
    /// </summary>
    public static bool TryParseDuration(string? input, out int seconds)
    {
        seconds = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var normalized = PersianDigits.ToLatin(input).Trim();

        var parts = normalized.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 1 or > 3) return false;

        var total = 0;
        foreach (var part in parts)
        {
            if (!int.TryParse(part.Trim(), out var value) || value < 0) return false;
            total = total * 60 + value;
        }

        seconds = total;
        return total > 0;
    }
}