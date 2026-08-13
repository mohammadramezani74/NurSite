using NurSite.Domain.Common;
using NurSite.Domain.Enums;

namespace NurSite.Domain.Entities;

/// <summary>یک فایل صوتی سخنرانی.</summary>
public class Lecture : BaseEntity, IAuditable, ISoftDelete, ISeoAware
{
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Description { get; set; }

    /// <summary>
    /// نشانی فایل صوتی روی همین سرور، مثل /uploads/lectures/2026/08/….mp3
    /// اگر صوت جای دیگری میزبانی شود این خالی می‌ماند.
    /// </summary>
    public string? AudioPath { get; set; }

    /// <summary>
    /// نشانی کامل فایل صوتی روی سرور دیگر. برای آرشیوهایی که از قبل
    /// جای دیگری بوده‌اند یا فایل‌هایی که نمی‌خواهیم روی این سرور بنشینند.
    /// </summary>
    public string? ExternalAudioUrl { get; set; }

    /// <summary>نشانی نهایی پخش، هر کدام که پر باشد. در دیتابیس ستون ندارد.</summary>
    public string? AudioUrl => string.IsNullOrWhiteSpace(ExternalAudioUrl) ? AudioPath : ExternalAudioUrl;

    /// <summary>صوت روی سرور خودمان نیست، پس نه شمارش دانلود دارد نه کنترل دسترسی.</summary>
    public bool IsExternal => !string.IsNullOrWhiteSpace(ExternalAudioUrl);

    /// <summary>مدت به ثانیه — برای نمایش و برای AudioObject در نشانه‌گذاری ساختاریافته.</summary>
    public int DurationSeconds { get; set; }
    public long FileSizeBytes { get; set; }

    public int? SpeakerId { get; set; }
    public Speaker? Speaker { get; set; }

    public int? LectureSeriesId { get; set; }
    public LectureSeries? LectureSeries { get; set; }
    public int? EpisodeNumber { get; set; }

    public PublishStatus Status { get; set; } = PublishStatus.Draft;
    public DateTime? RecordedOnUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public int PlayCount { get; set; }
    public int DownloadCount { get; set; }
    public bool AllowDownload { get; set; } = true;

    /// <summary>متن یکسان‌شده عنوان و توضیح و نام سخنران، فقط برای جستجو.</summary>
    public string? SearchText { get; set; }

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? OgImagePath { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedById { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedById { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}