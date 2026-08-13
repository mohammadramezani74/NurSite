using System.Globalization;

namespace NurSite.Application.Services;

/// <summary>تبدیل ارقام فارسی و عربی به لاتین، برای خواندن ورودی کاربر.</summary>
public static class PersianDigits
{
    public static string ToLatin(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        return new string(input.Select(c =>
            c is >= '\u06F0' and <= '\u06F9' ? (char)(c - '\u06F0' + '0') :
            c is >= '\u0660' and <= '\u0669' ? (char)(c - '\u0660' + '0') :
            c).ToArray());
    }
}

/// <summary>
/// خواندن و نوشتن تاریخ شمسی به شکل ۱۴۰۵/۰۵/۳۰.
///
/// کاربر پنل تاریخ را شمسی وارد می‌کند ولی دیتابیس UTC نگه می‌دارد،
/// پس این تبدیل در هر فرمی که تاریخ دارد لازم می‌شود.
/// </summary>
public static class PersianDateText
{
    private static readonly PersianCalendar Calendar = new();

    /// <summary>تاریخ شمسی ۱۴۰۵/۰۵/۳۰ یا ۱۴۰۵-۵-۳۰ را می‌خواند و UTC برمی‌گرداند.</summary>
    public static bool TryParse(string? input, out DateTime utc)
    {
        utc = default;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var parts = PersianDigits.ToLatin(input)
            .Split('/', '-', '.', ' ')
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        if (parts.Length != 3) return false;
        if (!int.TryParse(parts[0], out var y) ||
            !int.TryParse(parts[1], out var m) ||
            !int.TryParse(parts[2], out var d)) return false;

        if (y is < 1300 or > 1500 || m is < 1 or > 12 || d is < 1 or > 31) return false;

        try
        {
            // تاریخ ضبط ساعت ندارد، پس نیمه‌شب در نظر گرفته می‌شود
            utc = DateTime.SpecifyKind(Calendar.ToDateTime(y, m, d, 0, 0, 0, 0), DateTimeKind.Utc);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            // مثلاً ۳۱ اسفند در سالی که ۳۰ روز دارد
            return false;
        }
    }

    /// <summary>برای پر کردن فیلد فرم. مثال: ۱۴۰۵/۰۵/۳۰</summary>
    public static string Format(DateTime? utc)
    {
        if (utc is null) return string.Empty;

        var value = utc.Value;
        return $"{Calendar.GetYear(value)}/{Calendar.GetMonth(value):00}/{Calendar.GetDayOfMonth(value):00}";
    }
}