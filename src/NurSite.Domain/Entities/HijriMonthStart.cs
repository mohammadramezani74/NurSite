using NurSite.Domain.Common;

namespace NurSite.Domain.Entities;

/// <summary>
/// آغاز یک ماه قمری در تقویم رسمی ایران.
///
/// محاسبه خودکار بر مبنای تقویم ام‌القری است که تقویم عربستان است و در
/// تشخیص ۲۹ یا ۳۰ روزه بودن ماه با تقویم ایران اختلاف پیدا می‌کند. این
/// اختلاف ماه به ماه فرق می‌کند، پس با یک عدد ثابت قابل جبران نیست.
///
/// با ثبت تاریخ میلادی روز اول هر ماه قمری، تاریخ همه مناسبت‌های آن ماه
/// دقیق محاسبه می‌شود. اگر ماهی ثبت نشده باشد، سیستم به محاسبه ام‌القری
/// برمی‌گردد.
/// </summary>
public class HijriMonthStart : BaseEntity
{
    /// <summary>سال قمری، مثلاً ۱۴۴۸.</summary>
    public int HijriYear { get; set; }

    /// <summary>ماه قمری، از ۱ (محرم) تا ۱۲ (ذی‌الحجه).</summary>
    public int HijriMonth { get; set; }

    /// <summary>
    /// تاریخ میلادی روزی که این ماه قمری آغاز می‌شود.
    /// فقط بخش تاریخ اهمیت دارد، نه ساعت.
    /// </summary>
    public DateOnly StartsOn { get; set; }

    /// <summary>یادداشت ادمین، مثلاً منبعی که از آن گرفته شده.</summary>
    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedById { get; set; }
}