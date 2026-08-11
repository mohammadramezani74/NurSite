using System.Text;
using System.Text.RegularExpressions;

namespace NurSite.Application.Services;

/// <summary>
/// یکسان‌سازی متن فارسی برای جستجو.
/// یک کلمه فارسی به چند شکل نوشته می‌شود — با ی عربی یا فارسی، با نیم‌فاصله
/// یا بدون آن، با اعراب یا بی‌اعراب. بدون یکسان‌سازی، کاربری که «نماز مسافر»
/// می‌نویسد حکمی با نگارش «نمازِ مسافر» را پیدا نمی‌کند.
/// </summary>
public static partial class PersianText
{
    [GeneratedRegex(@"<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTags();

    [GeneratedRegex(@"[\u064B-\u065F\u0670\u0640]", RegexOptions.Compiled)]
    private static partial Regex Diacritics();

    [GeneratedRegex(@"[^\p{L}\p{Nd}\s]", RegexOptions.Compiled)]
    private static partial Regex Punctuation();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex Whitespace();

    /// <summary>متن را به شکل استانداردِ قابل جستجو درمی‌آورد.</summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var text = HtmlTags().Replace(input, " ");
        text = System.Net.WebUtility.HtmlDecode(text);

        // اعراب و کشیده حذف می‌شوند
        text = Diacritics().Replace(text, string.Empty);

        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            sb.Append(ch switch
            {
                // حروف عربی به فارسی
                'ي' or 'ى' or 'ئ' => 'ی',
                'ك' => 'ک',
                'ة' => 'ه',
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
                'ؤ' => 'و',

                // نیم‌فاصله و فاصله‌های ناپیدا به فاصله عادی
                '\u200C' or '\u200D' or '\u200E' or '\u200F' or '\u00A0' => ' ',

                // ارقام فارسی و عربی به لاتین
                >= '\u06F0' and <= '\u06F9' => (char)(ch - '\u06F0' + '0'),
                >= '\u0660' and <= '\u0669' => (char)(ch - '\u0660' + '0'),

                _ => char.ToLowerInvariant(ch)
            });
        }

        text = Punctuation().Replace(sb.ToString(), " ");
        text = Whitespace().Replace(text, " ").Trim();

        return text;
    }

    /// <summary>
    /// عبارت جستجو را به واژه‌های معنادار می‌شکند.
    /// واژه‌های خیلی رایج حذف می‌شوند چون در همه متن‌ها هستند و
    /// نتیجه را بی‌ربط می‌کنند.
    /// </summary>
    public static string[] Tokenize(string? query, int maxTerms = 6)
    {
        var normalized = Normalize(query);
        if (normalized.Length == 0) return [];

        return normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(term => term.Length > 1 && !StopWords.Contains(term))
            .Distinct()
            .Take(maxTerms)
            .ToArray();
    }

    private static readonly HashSet<string> StopWords =
    [
        "از", "به", "با", "در", "که", "را", "این", "آن", "است", "بود",
        "شد", "می", "هم", "یا", "تا", "بر", "برای", "چه", "چی", "اگر",
        "ولی", "اما", "هر", "همه", "چون", "کرد", "دارد", "دارم", "کنم",
        "باید", "شود", "شده", "های", "ها", "یک", "من", "ما", "شما", "او"
    ];
}