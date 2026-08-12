using NurSite.Domain.Common;
using NurSite.Domain.Enums;

namespace NurSite.Domain.Entities;

public class RulingVerdict : BaseEntity
{
    public int RulingNodeId { get; set; }
    public RulingNode RulingNode { get; set; } = default!;

    /// <summary>متن حکم، مثلاً «پاک است» یا «نماز باطل است».</summary>
    public string Text { get; set; } = default!;

    public VerdictScope Scope { get; set; } = VerdictScope.All;

    public int SortOrder { get; set; }

    /// <summary>ارجاع به منبع این حکم خاص، مثلاً شماره مسئله.</summary>
    public string? SourceNote { get; set; }

    public ICollection<RulingVerdictMarja> Marjas { get; set; } = new List<RulingVerdictMarja>();
}
