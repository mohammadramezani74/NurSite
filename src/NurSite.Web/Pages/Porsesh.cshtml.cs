using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Application.Services;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;
using NurSite.Web.Helpers;

namespace NurSite.Web.Pages;

[AllowAnonymous]
public class PorseshModel(
    AppDbContext db,
    INotificationService notifications,
    ILogger<PorseshModel> logger) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public SelectList CategoryOptions { get; private set; } = default!;

    /// <summary>پس از ثبت موفق، کد رهگیری برای نمایش به کاربر.</summary>
    [TempData] public string? IssuedCode { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "متن پرسش را بنویسید")]
        [StringLength(4000, MinimumLength = 15,
            ErrorMessage = "پرسش باید بین ۱۵ تا ۴۰۰۰ کاراکتر باشد")]
        [Display(Name = "پرسش شما")]
        public string Body { get; set; } = default!;

        [Display(Name = "باب")]
        public int? RulingCategoryId { get; set; }

        [StringLength(150)]
        [Display(Name = "نام")]
        public string? SenderName { get; set; }

        [Required(ErrorMessage = "شماره موبایل را وارد کنید")]
        [IranianMobile]
        [Display(Name = "شماره موبایل")]
        public string SenderMobile { get; set; } = default!;

        [Display(Name = "اجازه انتشار در آرشیو عمومی")]
        public bool AllowPublish { get; set; } = true;

        /// <summary>
        /// تله هرزنامه. این فیلد در صفحه پنهان است و کاربر واقعی نمی‌بیندش،
        /// اما ربات‌ها همه فیلدها را پر می‌کنند.
        /// </summary>
        public string? Website { get; set; }
    }

    public async Task OnGetAsync(CancellationToken ct) => await LoadOptionsAsync(ct);

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadOptionsAsync(ct);

        // ربات تله را پر کرده — وانمود می‌کنیم موفق بوده تا متوجه نشود
        if (!string.IsNullOrWhiteSpace(Input.Website))
        {
            logger.LogWarning("فرم پرسش توسط ربات پر شد و نادیده گرفته شد.");
            IssuedCode = TrackingCode.Generate();
            return RedirectToPage("./Porsesh", new { sent = true });
        }

        if (!ModelState.IsValid) return Page();

        var mobile = MobileNumber.Normalize(Input.SenderMobile);
        if (mobile is null)
        {
            ModelState.AddModelError("Input.SenderMobile", "شماره موبایل معتبر نیست.");
            return Page();
        }

        var ipHash = HashIp(HttpContext.Connection.RemoteIpAddress?.ToString());

        // محدودیت ساده: از یک شماره، حداکثر سه پرسش در روز
        var since = DateTime.UtcNow.AddDays(-1);
        var recentCount = await db.UserQuestions
            .CountAsync(q => q.SenderMobile == mobile && q.CreatedAtUtc >= since, ct);

        if (recentCount >= 3)
        {
            ModelState.AddModelError(string.Empty,
                "از این شماره امروز سه پرسش ثبت شده است. لطفاً فردا دوباره تلاش کنید.");
            return Page();
        }

        var code = await GenerateUniqueCodeAsync(ct);

        var question = new UserQuestion
        {
            Body = Input.Body.Trim(),
            SenderName = Input.SenderName?.Trim(),
            SenderMobile = mobile,
            RulingCategoryId = Input.RulingCategoryId,
            AllowPublish = Input.AllowPublish,
            TrackingCode = code,
            SenderIpHash = ipHash,
            Status = QuestionStatus.New
        };

        db.UserQuestions.Add(question);
        await db.SaveChangesAsync(ct);

        await notifications.QuestionReceivedAsync(
            new NotificationTarget(mobile, null, question.SenderName), code, ct);

        logger.LogInformation("پرسش تازه با کد {Code} ثبت شد.", code);

        IssuedCode = code;
        return RedirectToPage("./Porsesh", new { sent = true });
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var candidate = TrackingCode.Generate();
            if (!await db.UserQuestions.AnyAsync(q => q.TrackingCode == candidate, ct))
                return candidate;
        }

        // احتمالش عملاً صفر است، ولی نباید حلقه بی‌پایان شود
        throw new InvalidOperationException("ساخت کد رهگیری یکتا ناموفق بود.");
    }

    private async Task LoadOptionsAsync(CancellationToken ct)
    {
        var categories = await db.RulingCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
            .ToListAsync(ct);

        CategoryOptions = new SelectList(categories, nameof(RulingCategory.Id), nameof(RulingCategory.Title));
    }

    /// <summary>نشانی IP خام ذخیره نمی‌شود؛ فقط درهم آن برای تشخیص تکرار.</summary>
    private static string? HashIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ip));
        return Convert.ToHexString(bytes)[..32];
    }
}