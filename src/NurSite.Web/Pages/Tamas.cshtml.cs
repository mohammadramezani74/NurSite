using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Entities;
using NurSite.Infrastructure.Persistence;
using NurSite.Web.Helpers;

namespace NurSite.Web.Pages;

[AllowAnonymous]
public class TamasModel(AppDbContext db, ILogger<TamasModel> logger) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public SiteSetting? Settings { get; private set; }
    public string BaseUrl { get; private set; } = "";

    [TempData] public bool Sent { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "نام خود را بنویسید")]
        [StringLength(150)]
        [Display(Name = "نام")]
        public string SenderName { get; set; } = default!;

        [Required(ErrorMessage = "شماره موبایل را وارد کنید")]
        [IranianMobile]
        [Display(Name = "شماره موبایل")]
        public string SenderMobile { get; set; } = default!;

        [StringLength(200)]
        [EmailAddress(ErrorMessage = "رایانامه معتبر نیست")]
        [Display(Name = "رایانامه")]
        public string? SenderEmail { get; set; }

        [StringLength(250)]
        [Display(Name = "موضوع")]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "متن پیام را بنویسید")]
        [StringLength(4000, MinimumLength = 10,
            ErrorMessage = "پیام باید بین ۱۰ تا ۴۰۰۰ کاراکتر باشد")]
        [Display(Name = "پیام")]
        public string Body { get; set; } = default!;

        /// <summary>تله هرزنامه — در صفحه پنهان است و کاربر واقعی نمی‌بیندش.</summary>
        public string? Website { get; set; }
    }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadAsync(ct);

        // ربات تله را پر کرده — وانمود می‌کنیم موفق بوده
        if (!string.IsNullOrWhiteSpace(Input.Website))
        {
            logger.LogWarning("فرم تماس توسط ربات پر شد و نادیده گرفته شد.");
            Sent = true;
            return RedirectToPage();
        }

        if (!ModelState.IsValid) return Page();

        var mobile = MobileNumber.Normalize(Input.SenderMobile);
        if (mobile is null)
        {
            ModelState.AddModelError("Input.SenderMobile", "شماره موبایل معتبر نیست.");
            return Page();
        }

        var ipHash = HashIp(HttpContext.Connection.RemoteIpAddress?.ToString());

        // محدودیت: از یک شماره حداکثر سه پیام در روز
        var since = DateTime.UtcNow.AddDays(-1);
        var recent = await db.ContactMessages
            .CountAsync(m => m.SenderMobile == mobile && m.CreatedAtUtc >= since, ct);

        if (recent >= 3)
        {
            ModelState.AddModelError(string.Empty,
                "از این شماره امروز سه پیام ثبت شده است. لطفاً فردا دوباره تلاش کنید.");
            return Page();
        }

        db.ContactMessages.Add(new ContactMessage
        {
            SenderName = Input.SenderName.Trim(),
            SenderMobile = mobile,
            SenderEmail = string.IsNullOrWhiteSpace(Input.SenderEmail) ? null : Input.SenderEmail.Trim(),
            Subject = string.IsNullOrWhiteSpace(Input.Subject) ? null : Input.Subject.Trim(),
            Body = Input.Body.Trim(),
            SenderIpHash = ipHash,
            IsRead = false
        });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("پیام تماس تازه‌ای ثبت شد.");

        Sent = true;
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        BaseUrl = (Settings?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
    }

    private static string? HashIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ip));
        return Convert.ToHexString(bytes)[..32];
    }

    /// <summary>نشانه‌گذاری سازمان با اطلاعات تماس.</summary>
    public object BuildOrganizationSchema() => new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "Organization",
        ["name"] = Settings?.SiteName ?? "مؤسسه فرهنگی نورالثقلین",
        ["url"] = BaseUrl,
        ["telephone"] = Settings?.ContactPhone,
        ["email"] = Settings?.ContactEmail,
        ["address"] = string.IsNullOrWhiteSpace(Settings?.ContactAddress) ? null
            : new Dictionary<string, object>
            {
                ["@type"] = "PostalAddress",
                ["streetAddress"] = Settings.ContactAddress,
                ["addressCountry"] = "IR"
            },
        ["contactPoint"] = new Dictionary<string, object?>
        {
            ["@type"] = "ContactPoint",
            ["contactType"] = "customer support",
            ["telephone"] = Settings?.ContactPhone,
            ["availableLanguage"] = "Persian"
        }
    };
}