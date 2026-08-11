using NurSite.Domain.Common;
using NurSite.Domain.Enums;

namespace NurSite.Domain.Entities;

/// <summary>یک حکم شرعی به صورت پرسش و پاسخ. مبنای ساخت FAQPage در نشانه‌گذاری ساختاریافته.</summary>
public class Ruling : BaseEntity, IAuditable, ISoftDelete, ISeoAware
{
    public string Question { get; set; } = default!;
    public string Answer { get; set; } = default!;
    public string Slug { get; set; } = default!;

    public int RulingCategoryId { get; set; }
    public RulingCategory RulingCategory { get; set; } = default!;

    public int? MarjaId { get; set; }
    public Marja? Marja { get; set; }

    /// <summary>عبارتی مثل «مطابق فتوای رهبری و آیت‌الله سیستانی» که زیر پاسخ نمایش داده می‌شود.</summary>
    public string? FatwaNote { get; set; }
    public string? SourceReference { get; set; }

    public PublishStatus Status { get; set; } = PublishStatus.Draft;
    public bool IsFrequentlyAsked { get; set; }
    public int SortOrder { get; set; }
    public int ViewCount { get; set; }

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? OgImagePath { get; set; }

    /// <summary>متن یکسان‌شده پرسش و پاسخ، فقط برای جستجو.</summary>
    public string? SearchText { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedById { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedById { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}