using NurSite.Domain.Common;

namespace NurSite.Domain.Entities;

/// <summary>
/// شهر، برای محاسبه اوقات شرعی. خود اوقات ذخیره نمی‌شود و روزانه محاسبه و کش می‌گردد.
/// </summary>
public class City : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? ProvinceName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    /// <summary>ارتفاع از سطح دریا به متر — روی زمان طلوع و غروب اثر دارد.</summary>
    public double Elevation { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
}
