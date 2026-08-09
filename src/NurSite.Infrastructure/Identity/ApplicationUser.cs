using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace NurSite.Infrastructure.Identity;

/// <summary>
/// کاربر سایت. ورود با شماره موبایل انجام می‌شود، پس UserName همان موبایل نرمال‌شده است.
/// </summary>
public class ApplicationUser : IdentityUser
{
    [MaxLength(150)]
    public string? FullName { get; set; }

    [MaxLength(400)]
    public string? AvatarPath { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>پوسته انتخابی کاربر، برای همگام‌سازی بین دستگاه‌ها.</summary>
    [MaxLength(20)]
    public string? PreferredTheme { get; set; }
    public int? PreferredCityId { get; set; }
}