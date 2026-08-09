namespace NurSite.Application.Interfaces;

public interface IPersianDateService
{
    /// <summary>تاریخ UTC را به رشته شمسی تبدیل می‌کند. مثال: ۱۸ مرداد ۱۴۰۵</summary>
    string ToPersianDate(DateTime utc, bool includeWeekday = false, bool includeTime = false);

    /// <summary>تاریخ UTC را به رشته قمری تبدیل می‌کند. مثال: ١٥ صفر ١٤٤٨</summary>
    string ToHijriDate(DateTime utc);

    /// <summary>ارقام لاتین را به ارقام فارسی تبدیل می‌کند.</summary>
    string ToPersianDigits(string input);

    /// <summary>فاصله تا یک زمان را به فارسی روان برمی‌گرداند. مثال: ۶ ساعت و ۴۴ دقیقه</summary>
    string HumanizeUntil(DateTime targetUtc, DateTime? nowUtc = null);
}
