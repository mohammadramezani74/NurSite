using NurSite.Domain.Common;

namespace NurSite.Domain.Entities;

public class Photo : BaseEntity
{
    public int AlbumId { get; set; }
    public Album Album { get; set; } = default!;

    public string FilePath { get; set; } = default!;
    /// <summary>متن جایگزین تصویر. برای دسترس‌پذیری و سئوی تصاویر الزامی است.</summary>
    public string AltText { get; set; } = default!;
    public string? Caption { get; set; }
    /// <summary>ابعاد ذخیره می‌شود تا در HTML با width/height رندر شود و چیدمان نپرد.</summary>
    public int Width { get; set; }
    public int Height { get; set; }
    public int SortOrder { get; set; }
}
