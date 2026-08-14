using NurSite.Domain.Enums;

namespace NurSite.Application.Services;

/// <summary>
/// برچسب‌ها و نسبت ابعاد هر نوع رسانه، یک جا — مثل AudioKinds.
/// </summary>
public static class MediaKinds
{
    public static readonly IReadOnlyList<MediaKind> All =
        [MediaKind.Poster, MediaKind.Story, MediaKind.Wallpaper, MediaKind.Banner, MediaKind.Reel];

    /// <summary>نام مفرد. مثال: پوستر</summary>
    public static string Label(MediaKind kind) => kind switch
    {
        MediaKind.Story => "استوری",
        MediaKind.Wallpaper => "والپیپر",
        MediaKind.Banner => "بنر",
        MediaKind.Reel => "کلیپ",
        _ => "پوستر"
    };

    /// <summary>نام بخش. مثال: پوسترها</summary>
    public static string PluralLabel(MediaKind kind) => kind switch
    {
        MediaKind.Story => "استوری‌ها",
        MediaKind.Wallpaper => "والپیپرها",
        MediaKind.Banner => "بنرها",
        MediaKind.Reel => "کلیپ‌ها",
        _ => "پوسترها"
    };

    /// <summary>بخش نشانی. مثال: posterha</summary>
    public static string SectionSlug(MediaKind kind) => kind switch
    {
        MediaKind.Story => "esteriha",
        MediaKind.Wallpaper => "valpeiperha",
        MediaKind.Banner => "bannerha",
        MediaKind.Reel => "kelipha",
        _ => "posterha"
    };

    public static MediaKind? FromSectionSlug(string? section) => section?.ToLowerInvariant() switch
    {
        "posterha" => MediaKind.Poster,
        "esteriha" => MediaKind.Story,
        "valpeiperha" => MediaKind.Wallpaper,
        "bannerha" => MediaKind.Banner,
        "kelipha" => MediaKind.Reel,
        _ => null
    };

    /// <summary>
    /// نسبت پیشنهادی هر نوع، برای اینکه در فرم به کاربر گفته شود و
    /// در فهرست، جای تصویر پیش از بارگذاری درست نگه داشته شود.
    /// </summary>
    public static string AspectHint(MediaKind kind) => kind switch
    {
        MediaKind.Story or MediaKind.Reel => "۹ به ۱۶ — عمودی",
        MediaKind.Banner => "۱۶ به ۹ — افقی",
        MediaKind.Wallpaper => "عمودی برای گوشی یا افقی برای رایانه",
        _ => "عمودی، معمولاً ۳ به ۴"
    };

    /// <summary>حجم فایل به شکل خوانا. مثال: ۲٫۴ مگابایت</summary>
    public static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "";

        var mb = bytes / 1024d / 1024d;
        return mb >= 1
            ? $"{mb:0.#} مگابایت"
            : $"{bytes / 1024d:0} کیلوبایت";
    }
}