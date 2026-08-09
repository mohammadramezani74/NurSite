using NurSite.Domain.Common;

namespace NurSite.Domain.Entities;

/// <summary>
/// ریدایرکت دائمی. وقتی اسلاگ یک محتوا عوض می‌شود، آدرس قدیمی اینجا ثبت می‌گردد
/// تا رتبه‌ای که در گوگل گرفته از دست نرود.
/// </summary>
public class UrlRedirect : BaseEntity
{
    public string FromPath { get; set; } = default!;
    public string ToPath { get; set; } = default!;
    public int StatusCode { get; set; } = 301;
    public bool IsActive { get; set; } = true;
    public int HitCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
