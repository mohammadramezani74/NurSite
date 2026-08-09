using NurSite.Domain.Common;

namespace NurSite.Domain.Entities;

public class ContactMessage : BaseEntity, IAuditable
{
    public string SenderName { get; set; } = default!;
    public string? SenderMobile { get; set; }
    public string? SenderEmail { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = default!;
    public bool IsRead { get; set; }
    public string? AdminNote { get; set; }

    /// <summary>برای پیگیری هرزنامه نگه داشته می‌شود.</summary>
    public string? SenderIpHash { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedById { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedById { get; set; }
}
