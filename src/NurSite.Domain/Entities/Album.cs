using NurSite.Domain.Common;
using NurSite.Domain.Enums;

namespace NurSite.Domain.Entities;

public class Album : BaseEntity, IAuditable, ISeoAware
{
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Description { get; set; }
    public string? CoverImagePath { get; set; }
    public DateTime? TakenOnUtc { get; set; }
    public PublishStatus Status { get; set; } = PublishStatus.Draft;

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? OgImagePath { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedById { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedById { get; set; }

    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}
