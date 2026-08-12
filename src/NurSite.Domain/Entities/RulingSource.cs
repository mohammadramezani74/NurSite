using NurSite.Domain.Common;

namespace NurSite.Domain.Entities;

/// <summary>
/// منبع احکام — معمولاً یک کتاب.
///
/// وقتی احکام از اثر دیگری وارد می‌شود، باید به منبع اصلی ارجاع داده شود.
/// این موجودیت اطلاعات کتاب‌شناختی و نشانی خرید یا مشاهده را نگه می‌دارد.
/// </summary>
public class RulingSource : BaseEntity
{
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;

    public string? Author { get; set; }
    public string? Editor { get; set; }
    public string? Publisher { get; set; }

    /// <summary>سال انتشار به شمسی.</summary>
    public int? PublishedYear { get; set; }

    public string? Isbn { get; set; }
    public string? Edition { get; set; }

    /// <summary>نشانی صفحه کتاب برای معرفی یا خرید.</summary>
    public string? Url { get; set; }

    public string? CoverImagePath { get; set; }
    public string? Description { get; set; }

    /// <summary>متن اجازه‌نامه یا شرایطی که ناشر گذاشته است.</summary>
    public string? PermissionNote { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<Ruling> Rulings { get; set; } = new List<Ruling>();
}