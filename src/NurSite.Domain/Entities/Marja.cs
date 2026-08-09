using NurSite.Domain.Common;

namespace NurSite.Domain.Entities;

/// <summary>مرجع تقلید — پاسخ هر حکم به فتوای یک یا چند مرجع مستند می‌شود.</summary>
public class Marja : BaseEntity
{
    public string FullName { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? PortraitPath { get; set; }
    public string? OfficialSiteUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Ruling> Rulings { get; set; } = new List<Ruling>();
}
