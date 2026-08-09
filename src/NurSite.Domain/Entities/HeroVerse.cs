using NurSite.Domain.Common;

namespace NurSite.Domain.Entities;

/// <summary>آیه‌ای که در اسلایدر بالای صفحه اصلی نمایش داده می‌شود.</summary>
public class HeroVerse : BaseEntity
{
    /// <summary>متن عربی آیه.</summary>
    public string ArabicText { get; set; } = default!;
    /// <summary>ترجمه فارسی.</summary>
    public string PersianText { get; set; } = default!;
    /// <summary>منبع، مثل «سوره رعد · آیه ۲۸».</summary>
    public string Reference { get; set; } = default!;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
