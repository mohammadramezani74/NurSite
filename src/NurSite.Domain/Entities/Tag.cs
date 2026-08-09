using NurSite.Domain.Common;

namespace NurSite.Domain.Entities;

public class Tag : BaseEntity
{
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
}
