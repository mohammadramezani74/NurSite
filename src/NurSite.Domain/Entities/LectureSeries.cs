using NurSite.Domain.Common;

namespace NurSite.Domain.Entities;

/// <summary>مجموعه سخنرانی — مثل «شرح دعای ابوحمزه ثمالی» که چند جلسه دارد.</summary>
public class LectureSeries : BaseEntity, ISeoAware
{
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Description { get; set; }
    public string? CoverImagePath { get; set; }

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? OgImagePath { get; set; }

    public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
}
