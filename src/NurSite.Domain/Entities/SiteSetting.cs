using NurSite.Domain.Common;
using NurSite.Domain.Enums;

namespace NurSite.Domain.Entities;

/// <summary>
/// تنظیمات عمومی سایت. این جدول همیشه فقط یک رکورد با شناسه ۱ دارد.
/// </summary>
public class SiteSetting : BaseEntity
{
    public string SiteName { get; set; } = "مؤسسه فرهنگی نورالثقلین";
    public string? Tagline { get; set; }
    public string? LogoPath { get; set; }
    public string? FaviconPath { get; set; }

    public string? DefaultMetaTitle { get; set; }
    public string? DefaultMetaDescription { get; set; }
    public string? DefaultOgImagePath { get; set; }
    /// <summary>آدرس مبنای سایت — برای ساخت canonical و نقشه سایت لازم است.</summary>
    public string CanonicalBaseUrl { get; set; } = "https://example.ir";

    /// <summary>پوسته پیش‌فرض. کاربر می‌تواند در مرورگر خودش عوضش کند.</summary>
    public SiteTheme DefaultTheme { get; set; } = SiteTheme.Lajvard;
    /// <summary>اگر خاموش باشد، انتخاب پوسته به کاربر نمایش داده نمی‌شود.</summary>
    public bool AllowUserThemeChoice { get; set; } = true;
    /// <summary>اجازه بده مناسبت‌های عزا پوسته را خودکار عوض کنند.</summary>
    public bool EnableOccasionTheme { get; set; } = true;

    public int? DefaultCityId { get; set; }
    public City? DefaultCity { get; set; }

    public string? ContactAddress { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? WorkingHours { get; set; }
    public string? TelegramUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? AparatUrl { get; set; }

    public bool IsMaintenanceMode { get; set; }
}
