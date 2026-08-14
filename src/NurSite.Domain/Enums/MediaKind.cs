namespace NurSite.Domain.Enums;

/// <summary>
/// نوع رسانه در گالری. همه در یک جدول می‌نشینند چون ساختارشان یکی است
/// (فایل، ابعاد، آلبوم، ترتیب) و فقط کاربردشان فرق می‌کند.
/// </summary>
public enum MediaKind
{
    /// <summary>پوستر مناسبتی، معمولاً عمودی و برای چاپ یا انتشار.</summary>
    Poster = 0,

    /// <summary>استوری، نسبت ۹ به ۱۶ برای شبکه‌های اجتماعی.</summary>
    Story = 1,

    /// <summary>والپیپر و پس‌زمینه گوشی یا رایانه.</summary>
    Wallpaper = 2,

    /// <summary>بنر افقی، برای کانال و سربرگ.</summary>
    Banner = 3,

    /// <summary>کلیپ کوتاه. تصویرش کاور ویدیو است، نه خود اثر.</summary>
    Reel = 4
}