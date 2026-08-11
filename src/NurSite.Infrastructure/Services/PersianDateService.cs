using System.Globalization;
using System.Text;
using NurSite.Application.Interfaces;

namespace NurSite.Infrastructure.Services;

/// <summary>
/// تبدیل تاریخ به شمسی و قمری. ورودی همیشه UTC است و خروجی به وقت ایران محاسبه می‌شود.
/// </summary>
public sealed class PersianDateService : IPersianDateService
{
    private static readonly PersianCalendar Persian = new();
    private static readonly UmAlQuraCalendar Hijri = new();

    private static readonly string[] PersianMonths =
    {
        "فروردین","اردیبهشت","خرداد","تیر","مرداد","شهریور",
        "مهر","آبان","آذر","دی","بهمن","اسفند"
    };

    private static readonly string[] HijriMonths =
    {
        "محرم","صفر","ربیع‌الأول","ربیع‌الثانی","جمادی‌الأول","جمادی‌الثانی",
        "رجب","شعبان","رمضان","شوال","ذی‌القعده","ذی‌الحجه"
    };

    private static readonly string[] Weekdays =
    {
        "یکشنبه","دوشنبه","سه‌شنبه","چهارشنبه","پنجشنبه","جمعه","شنبه"
    };

    private static readonly TimeZoneInfo IranTz = ResolveIranTimeZone();

    private static TimeZoneInfo ResolveIranTimeZone()
    {
        // شناسه منطقه زمانی روی ویندوز و لینوکس فرق می‌کند
        foreach (var id in new[] { "Iran Standard Time", "Asia/Tehran" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        // اگر هیچ‌کدام نبود، آفست ثابت +۳:۳۰
        return TimeZoneInfo.CreateCustomTimeZone("Iran-Fallback", TimeSpan.FromMinutes(210), "Iran", "Iran");
    }

    private static DateTime ToLocal(DateTime utc)
    {
        if (utc.Kind == DateTimeKind.Unspecified)
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc.ToUniversalTime(), IranTz);
    }

    public string ToPersianDate(DateTime utc, bool includeWeekday = false, bool includeTime = false)
    {
        var local = ToLocal(utc);
        var year = Persian.GetYear(local);
        var month = Persian.GetMonth(local);
        var day = Persian.GetDayOfMonth(local);

        var sb = new StringBuilder();
        if (includeWeekday)
            sb.Append(Weekdays[(int)Persian.GetDayOfWeek(local)]).Append(' ');

        sb.Append(day).Append(' ').Append(PersianMonths[month - 1]).Append(' ').Append(year);

        if (includeTime)
            sb.Append(" ساعت ").Append(local.ToString("HH:mm", CultureInfo.InvariantCulture));

        return ToPersianDigits(sb.ToString());
    }

    public string ToHijriDate(DateTime utc, int dayOffset = 1)
    {
        // تقویم ام‌القری معمولاً یک روز با تقویم قمری ایران فاصله دارد
        var local = ToLocal(utc).AddDays(dayOffset);
        var year = Hijri.GetYear(local);
        var month = Hijri.GetMonth(local);
        var day = Hijri.GetDayOfMonth(local);
        return ToArabicDigits($"{day} {HijriMonths[month - 1]} {year}");
    }

    public string ToPersianDigits(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
            sb.Append(ch is >= '0' and <= '9' ? (char)(ch - '0' + '\u06F0') : ch);
        return sb.ToString();
    }

    /// <summary>ارقام عربی-هندی برای تاریخ قمری، که با متن عربی هماهنگ‌تر است.</summary>
    private static string ToArabicDigits(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
            sb.Append(ch is >= '0' and <= '9' ? (char)(ch - '0' + '\u0660') : ch);
        return sb.ToString();
    }

    public string HumanizeUntil(DateTime targetUtc, DateTime? nowUtc = null)
    {
        var span = targetUtc - (nowUtc ?? DateTime.UtcNow);
        if (span <= TimeSpan.Zero) return "هم‌اکنون";

        if (span.TotalDays >= 1)
        {
            var days = (int)span.TotalDays;
            var hours = span.Hours;
            return ToPersianDigits(hours > 0 ? $"{days} روز و {hours} ساعت" : $"{days} روز");
        }
        if (span.TotalHours >= 1)
        {
            var hours = (int)span.TotalHours;
            var minutes = span.Minutes;
            return ToPersianDigits(minutes > 0 ? $"{hours} ساعت و {minutes} دقیقه" : $"{hours} ساعت");
        }
        return ToPersianDigits($"{Math.Max(1, span.Minutes)} دقیقه");
    }
}