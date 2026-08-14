using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Infrastructure.Services;

/// <summary>
/// ساخت اسلاگ. حروف فارسی حفظ می‌شوند چون گوگل آدرس‌های فارسی را
/// درست ایندکس می‌کند و در نتایج فارسی خواناتر است.
/// </summary>
public sealed partial class SlugService(AppDbContext db) : ISlugService
{
    [GeneratedRegex(@"[^\p{L}\p{Nd}\-]+", RegexOptions.Compiled)]
    private static partial Regex NonSlugChars();

    [GeneratedRegex(@"-{2,}", RegexOptions.Compiled)]
    private static partial Regex RepeatedDashes();

    public string Generate(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;

        var normalized = NormalizePersian(title.Trim());
        normalized = normalized.Replace(' ', '-').Replace('\u200C', '-'); // نیم‌فاصله هم خط تیره می‌شود
        normalized = NonSlugChars().Replace(normalized, "-");
        normalized = RepeatedDashes().Replace(normalized, "-");
        return normalized.Trim('-').ToLowerInvariant();
    }

    /// <summary>یکسان‌سازی حروف عربی و فارسی تا اسلاگ‌های تکراری با املای متفاوت ساخته نشود.</summary>
    private static string NormalizePersian(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            sb.Append(ch switch
            {
                // حروف عربی که معادل فارسی دارند
                'ي' or 'ى' => 'ی',
                'ك' => 'ک',
                'ؤ' => 'و',
                'إ' or 'أ' => 'ا',
                'ة' => 'ه',

                // توجه: «آ» عمداً تبدیل نمی‌شود.
                // در فارسی حرف مستقلی است و تبدیلش به «ا» معنای کلمه را
                // عوض می‌کند — «آتش» به «اتش» تبدیل می‌شود.
                _ => ch
            });
        }
        return sb.ToString();
    }

    public async Task<string> GenerateUniqueAsync<TEntity>(string title, int? excludeId = null, CancellationToken ct = default)
    {
        var baseSlug = Generate(title);
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "item";

        var candidate = baseSlug;
        var counter = 2;

        while (await SlugExistsAsync<TEntity>(candidate, excludeId, ct))
        {
            candidate = $"{baseSlug}-{counter}";
            counter++;
        }
        return candidate;
    }

    private async Task<bool> SlugExistsAsync<TEntity>(string slug, int? excludeId, CancellationToken ct)
    {
        // بررسی عمومی روی هر موجودیتی که فیلد Slug دارد
        return typeof(TEntity).Name switch
        {
            nameof(Domain.Entities.Article) =>
                await db.Articles.AnyAsync(x => x.Slug == slug && (excludeId == null || x.Id != excludeId), ct),
            nameof(Domain.Entities.Ruling) =>
                await db.Rulings.AnyAsync(x => x.Slug == slug && (excludeId == null || x.Id != excludeId), ct),
            nameof(Domain.Entities.Lecture) =>
                await db.Lectures.AnyAsync(x => x.Slug == slug && (excludeId == null || x.Id != excludeId), ct),
            nameof(Domain.Entities.Speaker) =>
                await db.Speakers.AnyAsync(x => x.Slug == slug && (excludeId == null || x.Id != excludeId), ct),
            nameof(Domain.Entities.LectureSeries) =>
                await db.LectureSeries.AnyAsync(x => x.Slug == slug && (excludeId == null || x.Id != excludeId), ct),
            nameof(Domain.Entities.Event) =>
                await db.Events.AnyAsync(x => x.Slug == slug && (excludeId == null || x.Id != excludeId), ct),
            nameof(Domain.Entities.Category) =>
                await db.Categories.AnyAsync(x => x.Slug == slug && (excludeId == null || x.Id != excludeId), ct),
            nameof(Domain.Entities.Album) =>
                await db.Albums.AnyAsync(x => x.Slug == slug && (excludeId == null || x.Id != excludeId), ct),
            nameof(Domain.Entities.Photo) =>
                await db.Photos.AnyAsync(x => x.Slug == slug && (excludeId == null || x.Id != excludeId), ct),
            nameof(Domain.Entities.Marja) =>
                await db.Marjas.AnyAsync(x => x.Slug == slug && (excludeId == null || x.Id != excludeId), ct),
            nameof(Domain.Entities.RulingCategory) =>
                await db.RulingCategories.AnyAsync(x => x.Slug == slug && (excludeId == null || x.Id != excludeId), ct),
            nameof(Domain.Entities.RulingSource) =>
                await db.RulingSources.AnyAsync(x => x.Slug == slug && (excludeId == null || x.Id != excludeId), ct),
            nameof(Domain.Entities.Tag) =>
                await db.Tags.AnyAsync(x => x.Slug == slug && (excludeId == null || x.Id != excludeId), ct),
            _ => false
        };
    }
}