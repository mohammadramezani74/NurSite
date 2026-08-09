using NurSite.Domain.Common;
using NurSite.Domain.Enums;

namespace NurSite.Domain.Entities;

/// <summary>
/// مناسبت قمری. تاریخ به صورت روز و ماه قمری ذخیره می‌شود، نه تاریخ میلادی،
/// چون هر سال تکرار می‌شود و معادل شمسی‌اش سال به سال فرق می‌کند.
/// </summary>
public class Occasion : BaseEntity
{
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Description { get; set; }

    /// <summary>ماه قمری، از ۱ (محرم) تا ۱۲ (ذی‌الحجه).</summary>
    public int HijriMonth { get; set; }
    /// <summary>روز قمری، ۱ تا ۳۰.</summary>
    public int HijriDay { get; set; }

    public OccasionKind Kind { get; set; }
    public bool IsPublicHoliday { get; set; }

    /// <summary>اگر مقداری داشته باشد، پوسته سایت در این ایام خودکار عوض می‌شود.</summary>
    public SiteTheme? ForcedTheme { get; set; }
    /// <summary>چند روز قبل از مناسبت پوسته اعمال شود.</summary>
    public int ThemeStartsDaysBefore { get; set; }
    /// <summary>چند روز بعد از مناسبت پوسته برداشته شود.</summary>
    public int ThemeEndsDaysAfter { get; set; }

    public bool IsActive { get; set; } = true;
}
