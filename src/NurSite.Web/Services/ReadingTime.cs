using System.Text.RegularExpressions;

namespace NurSite.Web.Services;

/// <summary>
/// تخمین زمان مطالعه. سرعت خواندن فارسی حدود ۲۰۰ کلمه در دقیقه در نظر گرفته شده.
/// این عدد در صفحه مقاله نمایش داده می‌شود و در نشانه‌گذاری ساختاریافته هم می‌آید.
/// </summary>
public static partial class ReadingTime
{
    [GeneratedRegex("<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTags();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex Whitespace();

    private const int WordsPerMinute = 200;

    public static int Estimate(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return 1;

        var text = HtmlTags().Replace(html, " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = Whitespace().Replace(text, " ").Trim();

        if (text.Length == 0) return 1;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling(words / (double)WordsPerMinute));
    }

    /// <summary>خلاصه خودکار از ابتدای متن، برای وقتی نویسنده توضیح متا ننوشته باشد.</summary>
    public static string Excerpt(string? html, int maxLength = 160)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        var text = HtmlTags().Replace(html, " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = Whitespace().Replace(text, " ").Trim();

        if (text.Length <= maxLength) return text;

        // یک کاراکتر برای سه‌نقطه کنار گذاشته می‌شود تا خروجی
        // هرگز از maxLength بیشتر نشود
        var cut = text[..(maxLength - 1)];

        // در مرز کلمه ببر، نه وسط کلمه
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > maxLength / 2) cut = cut[..lastSpace];

        return cut.TrimEnd() + "…";
    }
}