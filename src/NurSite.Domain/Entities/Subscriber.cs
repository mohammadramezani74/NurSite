using NurSite.Domain.Common;

namespace NurSite.Domain.Entities;

/// <summary>عضو خبرنامه پیامکی.</summary>
public class Subscriber : BaseEntity
{
    public string Mobile { get; set; } = default!;
    public string? DisplayName { get; set; }
    public DateTime SubscribedAtUtc { get; set; }
    public DateTime? UnsubscribedAtUtc { get; set; }
    public bool IsConfirmed { get; set; }
    /// <summary>توکن یکبارمصرف تأیید یا لغو عضویت.</summary>
    public string? ConfirmationToken { get; set; }
    public bool IsActive => UnsubscribedAtUtc is null;
}
