using NurSite.Domain.Common;
using NurSite.Domain.Enums;

namespace NurSite.Domain.Entities;

/// <summary>
/// یک قلم رسانه در گالری — پوستر، استوری، والپیپر، بنر یا کلیپ.
/// نام کلاس به احترام کدی که از قبل بوده «Photo» مانده است.
/// </summary>
public class Photo : BaseEntity
{
    public int AlbumId { get; set; }
    public Album Album { get; set; } = default!;

    public MediaKind Kind { get; set; } = MediaKind.Poster;

    /// <summary>عنوان قابل جستجو. کاربر «پوستر شهادت امام حسین» را می‌نویسد.</summary>
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;

    /// <summary>
    /// نشانی تصویر. برای کلیپ، همین کاور ویدیوست نه خود ویدیو —
    /// تا فهرست گالری بدون بارگذاری ویدیو قابل نمایش باشد.
    /// </summary>
    public string FilePath { get; set; } = default!;

    /// <summary>متن جایگزین تصویر. برای دسترس‌پذیری و سئوی تصاویر الزامی است.</summary>
    public string AltText { get; set; } = default!;
    public string? Caption { get; set; }

    /// <summary>ابعاد ذخیره می‌شود تا در HTML با width/height رندر شود و چیدمان نپرد.</summary>
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSizeBytes { get; set; }

    /// <summary>ویدیو روی همین سرور، اگر آپلود شده باشد.</summary>
    public string? VideoPath { get; set; }

    /// <summary>ویدیو روی سرور دیگر — آپارات، اینستاگرام یا هر جای دیگر.</summary>
    public string? ExternalVideoUrl { get; set; }

    /// <summary>نشانی نهایی ویدیو، هر کدام که پر باشد. در دیتابیس ستون ندارد.</summary>
    public string? VideoUrl => string.IsNullOrWhiteSpace(ExternalVideoUrl) ? VideoPath : ExternalVideoUrl;

    public bool HasVideo => !string.IsNullOrWhiteSpace(VideoUrl);

    /// <summary>ویدیو روی سرور خودمان نیست، پس شمارش دانلود ندارد.</summary>
    public bool IsExternalVideo => !string.IsNullOrWhiteSpace(ExternalVideoUrl);

    public int DownloadCount { get; set; }
    public int SortOrder { get; set; }
}
