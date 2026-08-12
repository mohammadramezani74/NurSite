using NurSite.Domain.Common;
using NurSite.Domain.Enums;

namespace NurSite.Domain.Entities;

/// <summary>یک حکم شرعی به صورت پرسش و پاسخ. مبنای ساخت FAQPage در نشانه‌گذاری ساختاریافته.</summary>
public class Ruling : BaseEntity, IAuditable, ISoftDelete, ISeoAware
{
    public string Question { get; set; } = default!;

    /// <summary>
    /// پاسخ متنی. در احکام نموداری خالی یا خلاصه است و محتوای اصلی
    /// در درخت Nodes قرار دارد.
    /// </summary>
    public string Answer { get; set; } = default!;
    public string Slug { get; set; } = default!;

    public int RulingCategoryId { get; set; }
    public RulingCategory RulingCategory { get; set; } = default!;

    /// <summary>
    /// مرجع تقلید، برای احکام تک‌مرجعی.
    /// در احکام نموداری که نظر مراجع متفاوت است، این خالی می‌ماند و
    /// مراجع در سطح هر شاخه از نمودار مشخص می‌شوند.
    /// </summary>
    public int? MarjaId { get; set; }
    public Marja? Marja { get; set; }

    /// <summary>منبع کتابی این حکم، اگر از اثر دیگری وارد شده باشد.</summary>
    public int? RulingSourceId { get; set; }
    public RulingSource? RulingSource { get; set; }

    /// <summary>شماره صفحه در منبع.</summary>
    public string? SourcePage { get; set; }

    /// <summary>
    /// این حکم به‌جای پاسخ متنی، نمودار شرطی دارد.
    /// وقتی درست باشد، صفحه عمومی درخت را رندر می‌کند نه متن Answer را.
    /// </summary>
    public bool HasDiagram { get; set; }

    public ICollection<RulingNode> Nodes { get; set; } = new List<RulingNode>();

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
