namespace NurSite.Domain.Common;

/// <summary>پایه همه موجودیت‌ها.</summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}

/// <summary>موجودیتی که تاریخ ایجاد و ویرایش را نگه می‌دارد. تاریخ‌ها همیشه UTC هستند.</summary>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    string? CreatedById { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
    string? UpdatedById { get; set; }
}

/// <summary>موجودیتی که به‌جای حذف فیزیکی، علامت حذف می‌خورد.</summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
}

/// <summary>موجودیتی که آدرس یکتای خوانا (اسلاگ) و متادیتای سئو دارد.</summary>
public interface ISeoAware
{
    string Slug { get; set; }
    string? MetaTitle { get; set; }
    string? MetaDescription { get; set; }
    string? OgImagePath { get; set; }
}
