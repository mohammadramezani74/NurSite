using NurSite.Domain.Common;

namespace NurSite.Domain.Entities;

/// <summary>سخنران یا استاد.</summary>
public class Speaker : BaseEntity
{
    public string FullName { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? PortraitPath { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
}
