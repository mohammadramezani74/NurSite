namespace NurSite.Domain.Entities;

/// <summary>جدول واسط مقاله و برچسب. کلید ترکیبی دارد، پس از BaseEntity ارث نمی‌برد.</summary>
public class ArticleTag
{
    public int ArticleId { get; set; }
    public Article Article { get; set; } = default!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = default!;
}
