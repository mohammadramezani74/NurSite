using NurSite.Domain.Common;

namespace NurSite.Domain.Entities;

/// <summary>باب احکام: طهارت، نماز، روزه، خمس، ...</summary>
public class RulingCategory : BaseEntity, ISeoAware
{
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Description { get; set; }
    public string? IconName { get; set; }
    public int SortOrder { get; set; }

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? OgImagePath { get; set; }

    public ICollection<Ruling> Rulings { get; set; } = new List<Ruling>();
}
