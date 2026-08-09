using NurSite.Domain.Common;
using NurSite.Domain.Enums;

namespace NurSite.Domain.Entities;

/// <summary>مقاله یا یادداشت.</summary>
public class Article : BaseEntity, IAuditable, ISoftDelete, ISeoAware
{
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Summary { get; set; }
    public string Body { get; set; } = default!;
    public string? CoverImagePath { get; set; }
    public string? CoverImageAlt { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = default!;

    /// <summary>شناسه کاربر نویسنده. عمداً رشته است تا دامنه به Identity وابسته نشود.</summary>
    public string? AuthorId { get; set; }
    public string? AuthorDisplayName { get; set; }

    public PublishStatus Status { get; set; } = PublishStatus.Draft;
    public DateTime? PublishedAtUtc { get; set; }
    public bool IsFeatured { get; set; }
    public int ViewCount { get; set; }

    /// <summary>زمان تقریبی مطالعه به دقیقه — هنگام ذخیره از روی متن محاسبه می‌شود.</summary>
    public int ReadingMinutes { get; set; }

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? OgImagePath { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedById { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedById { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
}
