using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NurSite.Infrastructure.Identity;
using NurSite.Infrastructure.Services;
using NurSite.Web.Helpers;

namespace NurSite.Web.Pages;

/// <summary>
/// ورود با کد یک‌بارمصرف پیامکی.
///
/// دو مرحله دارد و هر دو در همین صفحه‌اند: شماره، بعد کد. شماره میان دو
/// مرحله در TempData نگه داشته می‌شود نه در فیلد پنهان فرم، تا کسی
/// نتواند مرحله دوم را برای شماره دیگری بفرستد.
///
/// اگر شماره در سایت نباشد، حساب تازه ساخته می‌شود — ثبت‌نام و ورود یک
/// مسیرند، چون کسی که کد پیامک‌شده را دارد مالک آن شماره است.
/// </summary>
[AllowAnonymous]
public class VoroodModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ILoginCodeService codes,
    ILogger<VoroodModel> logger) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    /// <summary>در مرحله دوم هستیم؟</summary>
    public bool AwaitingCode { get; private set; }

    /// <summary>شماره‌ای که کد برایش رفته، برای نمایش در مرحله دوم.</summary>
    public string? PendingMobile { get; private set; }

    /// <summary>چند ثانیه تا امکان ارسال دوباره.</summary>
    public int ResendIn { get; private set; }

    [TempData] public string? PendingMobileStore { get; set; }
    [TempData] public string? Flash { get; set; }

    public class InputModel
    {
        [Display(Name = "شماره موبایل")]
        public string? Mobile { get; set; }

        [Display(Name = "کد تأیید")]
        public string? Code { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        ReturnUrl = SafeReturnUrl(returnUrl);

        if (!string.IsNullOrEmpty(PendingMobileStore))
        {
            PendingMobile = PendingMobileStore;
            PendingMobileStore = PendingMobile;
            AwaitingCode = true;
            ResendIn = await codes.SecondsUntilResendAsync(PendingMobile);
        }

        return Page();
    }

    /// <summary>مرحله اول: گرفتن شماره و فرستادن کد.</summary>
    public async Task<IActionResult> OnPostSendAsync(string? returnUrl, CancellationToken ct)
    {
        ReturnUrl = SafeReturnUrl(returnUrl);

        var mobile = MobileNumber.Normalize(Input.Mobile);
        if (mobile is null)
        {
            ModelState.AddModelError("Input.Mobile", "شماره موبایل معتبر نیست.");
            return Page();
        }

        // حساب غیرفعال پیش از خرج کردن پیامک بررسی می‌شود
        var existing = await userManager.FindByNameAsync(mobile);
        if (existing is { IsActive: false })
        {
            ModelState.AddModelError(string.Empty,
                "حساب شما غیرفعال شده است. برای پیگیری با مدیریت تماس بگیرید.");
            return Page();
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await codes.RequestAsync(mobile, ip, ct);

        PendingMobile = mobile;
        AwaitingCode = true;
        ResendIn = result.RetryAfterSeconds;

        if (!result.Ok)
        {
            // «کد قبلی هنوز معتبر است» خطا نیست؛ کاربر باید همان را وارد کند
            if (result.RetryAfterSeconds > 0)
            {
                PendingMobileStore = mobile;
                Flash = result.Error;
                return Page();
            }

            ModelState.AddModelError(string.Empty, result.Error ?? "ارسال کد ممکن نشد.");
            AwaitingCode = false;
            return Page();
        }

        PendingMobileStore = mobile;
        Flash = "کد تأیید برای شما پیامک شد.";
        return Page();
    }

    /// <summary>مرحله دوم: بررسی کد و ورود.</summary>
    public async Task<IActionResult> OnPostVerifyAsync(string? returnUrl, CancellationToken ct)
    {
        ReturnUrl = SafeReturnUrl(returnUrl);

        var mobile = PendingMobileStore;
        if (string.IsNullOrEmpty(mobile))
        {
            // نشست منقضی شده یا کسی مستقیم مرحله دوم را صدا زده است
            return RedirectToPage("/Vorood", new { returnUrl = ReturnUrl });
        }

        PendingMobile = mobile;
        PendingMobileStore = mobile;
        AwaitingCode = true;

        if (string.IsNullOrWhiteSpace(Input.Code))
        {
            ModelState.AddModelError("Input.Code", "کد تأیید را وارد کنید.");
            ResendIn = await codes.SecondsUntilResendAsync(mobile, ct);
            return Page();
        }

        var status = await codes.VerifyAsync(mobile, Input.Code, ct);

        if (status != CodeCheckStatus.Ok)
        {
            ModelState.AddModelError("Input.Code", status switch
            {
                CodeCheckStatus.Expired => "این کد منقضی شده است. کد تازه بگیرید.",
                CodeCheckStatus.TooManyAttempts => "تعداد تلاش‌ها زیاد شد. کد تازه بگیرید.",
                CodeCheckStatus.NotFound => "کدی برای این شماره صادر نشده است.",
                _ => "کد وارد شده درست نیست."
            });

            ResendIn = await codes.SecondsUntilResendAsync(mobile, ct);
            return Page();
        }

        var user = await userManager.FindByNameAsync(mobile);

        if (user is null)
        {
            // نخستین ورود، همان ثبت‌نام است
            user = new ApplicationUser
            {
                UserName = mobile,
                PhoneNumber = mobile,
                PhoneNumberConfirmed = true,
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            var created = await userManager.CreateAsync(user);
            if (!created.Succeeded)
            {
                logger.LogError("ساخت کاربر {Mobile} ناموفق: {Errors}",
                    Mask(mobile), string.Join(" | ", created.Errors.Select(e => e.Description)));

                ModelState.AddModelError(string.Empty, "ساخت حساب ممکن نشد. با مدیریت تماس بگیرید.");
                return Page();
            }

            await userManager.AddToRoleAsync(user, AppRoles.Member);
            logger.LogInformation("کاربر تازه {UserId} ثبت شد", user.Id);
        }
        else if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty,
                "حساب شما غیرفعال شده است. برای پیگیری با مدیریت تماس بگیرید.");
            return Page();
        }

        // شماره با کد تأیید شد، پس تأییدشده علامت می‌خورد
        if (!user.PhoneNumberConfirmed)
        {
            user.PhoneNumberConfirmed = true;
            user.PhoneNumber = mobile;
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        await signInManager.SignInAsync(user, isPersistent: true);
        PendingMobileStore = null;

        logger.LogInformation("ورود موفق کاربر {UserId}", user.Id);

        if (ReturnUrl is not null) return LocalRedirect(ReturnUrl);

        // مقصد بر اساس دسترسی تعیین می‌شود نه نقش. با بررسی نقش، پاسخگوی
        // شرعی و ویراستار روی صفحه اصلی سایت رها می‌شدند، در حالی که
        // کارشان در پنل است.
        var hasPanelAccess = await HasAnyPermissionAsync(user);

        return hasPanelAccess
            ? RedirectToPage("/Index", new { area = "Admin" })
            : RedirectToPage("/Index");
    }

    /// <summary>
    /// کاربر از راه نقش‌هایش دست‌کم یک دسترسی دارد؟ همان شرطی که
    /// سیاست AdminArea هم بررسی می‌کند.
    /// </summary>
    private async Task<bool> HasAnyPermissionAsync(ApplicationUser user)
    {
        foreach (var roleName in await userManager.GetRolesAsync(user))
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var claims = await roleManager.GetClaimsAsync(role);
            if (claims.Any(c => c.Type == Permissions.ClaimType)) return true;
        }

        return false;
    }

    /// <summary>بازگشت به مرحله اول، برای وقتی که شماره اشتباه وارد شده.</summary>
    public IActionResult OnPostChangeMobile(string? returnUrl)
    {
        PendingMobileStore = null;
        return RedirectToPage("/Vorood", new { returnUrl = SafeReturnUrl(returnUrl) });
    }

    /// <summary>
    /// فقط مسیرهای داخلی پذیرفته می‌شوند. بدون این بررسی، مهاجم می‌تواند
    /// با لینک ?returnUrl=https://... کاربر را پس از ورود به سایت دیگری ببرد.
    /// </summary>
    private string? SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;

    private static string Mask(string mobile) =>
        mobile.Length < 7 ? "***" : $"{mobile[..4]}***{mobile[^2..]}";
}