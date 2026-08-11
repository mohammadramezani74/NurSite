using System.Security.Cryptography;

namespace NurSite.Application.Services;

/// <summary>
/// ساخت کد رهگیری. حروف و ارقامی که با هم اشتباه می‌شوند حذف شده‌اند —
/// صفر و O، یک و I و L — چون کاربر باید کد را از روی صفحه یادداشت کند
/// یا تلفنی بخواند.
/// </summary>
public static class TrackingCode
{
    private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
    private const int Length = 8;

    public static string Generate()
    {
        var chars = new char[Length];
        for (var i = 0; i < Length; i++)
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

        // به شکل ABCD-1234 نمایش داده می‌شود که خواندنش راحت‌تر است
        return $"{new string(chars, 0, 4)}-{new string(chars, 4, 4)}";
    }

    /// <summary>ورودی کاربر را برای مقایسه یکسان می‌کند.</summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var cleaned = new string(input
            .Where(c => char.IsLetterOrDigit(c))
            .Select(char.ToUpperInvariant)
            .ToArray());

        return cleaned.Length == 8 ? $"{cleaned[..4]}-{cleaned[4..]}" : cleaned;
    }
}