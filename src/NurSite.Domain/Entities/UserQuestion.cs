using NurSite.Domain.Common;
using NurSite.Domain.Enums;

namespace NurSite.Domain.Entities;

/// <summary>پرسش شرعی ارسال‌شده توسط کاربر.</summary>
public class UserQuestion : BaseEntity, IAuditable
{
    public string Body { get; set; } = default!;
    public string? SenderName { get; set; }
    public string? SenderMobile { get; set; }
    public string? SenderEmail { get; set; }

    /// <summary>
    /// کد رهگیری که هنگام ثبت به پرسشگر داده می‌شود تا بدون ورود به سایت
    /// پاسخش را پیگیری کند. حروف مبهم مثل O و 0 در آن نمی‌آید.
    /// </summary>
    public string TrackingCode { get; set; } = default!;

    /// <summary>پرسشگر اجازه داده پرسش و پاسخ در آرشیو عمومی منتشر شود.</summary>
    public bool AllowPublish { get; set; } = true;

    /// <summary>برای تشخیص هرزنامه. خودِ IP ذخیره نمی‌شود.</summary>
    public string? SenderIpHash { get; set; }

    public int? RulingCategoryId { get; set; }
    public RulingCategory? RulingCategory { get; set; }

    public QuestionStatus Status { get; set; } = QuestionStatus.New;
    public string? AssignedToUserId { get; set; }
    public string? AnswerBody { get; set; }
    public DateTime? AnsweredAtUtc { get; set; }

    /// <summary>اگر پاسخ به آرشیو عمومی منتقل شد، به حکم ساخته‌شده وصل می‌شود.</summary>
    public int? PublishedRulingId { get; set; }
    public Ruling? PublishedRuling { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedById { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedById { get; set; }
}