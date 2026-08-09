using System.Text;
using System.Text.RegularExpressions;

namespace NurSite.Web.Helpers;

/// <summary>
/// یکسان‌سازی شماره موبایل ایرانی. کاربر ممکن است شماره را با ارقام فارسی،
/// با پیش‌شماره +۹۸، با فاصله یا خط تیره وارد کند — همه به یک شکل استاندارد
/// «۰۹xxxxxxxxx» تبدیل می‌شوند تا نام کاربری همیشه یکتا بماند.
/// </summary>
public static partial class MobileNumber
{
    [GeneratedRegex(@"^09\d{9}$", RegexOptions.Compiled)]
    private static partial Regex ValidPattern();

    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            // ارقام فارسی ۰-۹
            if (ch is >= '\u06F0' and <= '\u06F9') sb.Append((char)(ch - '\u06F0' + '0'));
            // ارقام عربی-هندی ٠-٩
            else if (ch is >= '\u0660' and <= '\u0669') sb.Append((char)(ch - '\u0660' + '0'));
            else if (char.IsDigit(ch)) sb.Append(ch);
            else if (ch == '+') sb.Append(ch);
            // فاصله، خط تیره و پرانتز نادیده گرفته می‌شوند
        }

        var digits = sb.ToString();

        if (digits.StartsWith("+98")) digits = "0" + digits[3..];
        else if (digits.StartsWith("0098")) digits = "0" + digits[4..];
        else if (digits.StartsWith("98") && digits.Length == 12) digits = "0" + digits[2..];
        else if (digits.StartsWith('9') && digits.Length == 10) digits = "0" + digits;

        digits = digits.TrimStart('+');

        return IsValid(digits) ? digits : null;
    }

    public static bool IsValid(string? mobile) =>
        !string.IsNullOrEmpty(mobile) && ValidPattern().IsMatch(mobile);
}