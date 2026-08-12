using NurSite.Domain.Common;

namespace NurSite.Domain.Entities;

/// <summary>
/// یک گره از نمودار شرطی حکم.
///
/// احکام نموداری به شکل درخت تصمیم نوشته می‌شوند: «اگر چنین بود، آنگاه...
/// وگرنه اگر چنان بود...». هر گره یک شرط است و می‌تواند فرزندانی داشته باشد
/// یا مستقیماً به حکم برسد.
/// </summary>
public class RulingNode : BaseEntity
{
    public int RulingId { get; set; }
    public Ruling Ruling { get; set; } = default!;

    /// <summary>گره والد. خالی یعنی این گره در ریشه نمودار است.</summary>
    public int? ParentId { get; set; }
    public RulingNode? Parent { get; set; }

    /// <summary>متن شرط، مثلاً «ساخت کشور غیراسلامی است».</summary>
    public string Text { get; set; } = default!;

    /// <summary>ترتیب میان هم‌ترازها.</summary>
    public int SortOrder { get; set; }

    /// <summary>عمق در درخت — برای مرتب‌سازی و نمایش، از صفر شروع می‌شود.</summary>
    public int Depth { get; set; }

    /// <summary>پانویس یا توضیح تکمیلی این شاخه.</summary>
    public string? Note { get; set; }

    public ICollection<RulingNode> Children { get; set; } = new List<RulingNode>();
    public ICollection<RulingVerdict> Verdicts { get; set; } = new List<RulingVerdict>();
}
