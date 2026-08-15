using NurSite.Domain.Common;

namespace NurSite.Domain.Entities;

/// <summary>
/// کد یک‌بارمصرف ورود.
///
/// خودِ کد ذخیره نمی‌شود، فقط هش آن — تا اگر روزی کسی به دیتابیس دست
/// پیدا کرد، نتواند با کدهای در جریان وارد حساب کسی شود.
/// </summary>
public class LoginCode : BaseEntity
{
    /// <summary>شماره یکسان‌شده، به شکل ۰۹xxxxxxxxx.</summary>
    public string Mobile { get; set; } = default!;

    public string CodeHash { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>پس از استفاده پر می‌شود تا یک کد دو بار به کار نیاید.</summary>
    public DateTime? UsedAtUtc { get; set; }

    /// <summary>چند بار کد اشتباه وارد شده. برای جلوگیری از حدس زدن.</summary>
    public int FailedAttempts { get; set; }

    /// <summary>نشانی درخواست‌کننده، هش‌شده. فقط برای تشخیص ارسال انبوه.</summary>
    public string? IpHash { get; set; }

    public bool IsUsable(DateTime nowUtc) =>
        UsedAtUtc is null && FailedAttempts < 5 && nowUtc < ExpiresAtUtc;
}