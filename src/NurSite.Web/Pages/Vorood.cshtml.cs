using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NurSite.Infrastructure.Identity;
using NurSite.Web.Helpers;

namespace NurSite.Web.Pages;

[AllowAnonymous]
public class VoroodModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ILogger<VoroodModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    /// <summary>تا چه زمانی حساب قفل است. برای نمایش پیام دقیق‌تر.</summary>
    public DateTimeOffset? LockedUntil { get; private set; }

    public class InputModel
    {
        [Required(ErrorMessage = "شماره موبایل را وارد کنید")]
        [IranianMobile]
        [Display(Name = "شماره موبایل")]
        public string Mobile { get; set; } = default!;

        [Required(ErrorMessage = "رمز عبور را وارد کنید")]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; } = default!;

        [Display(Name = "مرا به خاطر بسپار")]
        public bool RememberMe { get; set; } = true;
    }

    public IActionResult OnGet(string? returnUrl = null)
    {
        // کاربری که قبلاً وارد شده، دوباره فرم ورود نبیند
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        ReturnUrl = SafeReturnUrl(returnUrl);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = SafeReturnUrl(returnUrl);

        if (!ModelState.IsValid) return Page();

        var mobile = MobileNumber.Normalize(Input.Mobile);
        if (mobile is null)
        {
            ModelState.AddModelError(string.Empty, "شماره موبایل معتبر نیست.");
            return Page();
        }

        var user = await userManager.FindByNameAsync(mobile);

        // پیام یکسان برای «کاربر نیست» و «رمز غلط» تا کسی نتواند
        // با آزمون و خطا بفهمد چه شماره‌هایی در سایت ثبت شده‌اند
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "شماره موبایل یا رمز عبور نادرست است.");
            return Page();
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty,
                "حساب شما غیرفعال شده است. برای پیگیری با مدیریت تماس بگیرید.");
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(
            user, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAtUtc = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            logger.LogInformation("ورود موفق کاربر {UserId}", user.Id);
            return LocalRedirect(ReturnUrl ?? Url.Page("/Index")!);
        }

        if (result.IsLockedOut)
        {
            LockedUntil = user.LockoutEnd;
            var minutes = user.LockoutEnd is null
                ? 15
                : Math.Max(1, (int)Math.Ceiling((user.LockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes));

            ModelState.AddModelError(string.Empty,
                $"به دلیل تلاش‌های ناموفق، حساب شما تا {minutes} دقیقه دیگر قفل است.");

            logger.LogWarning("حساب {UserId} قفل شد", user.Id);
            return Page();
        }

        if (result.RequiresTwoFactor)
            return RedirectToPage("/TaeedDoMarhaleie", new { returnUrl = ReturnUrl, Input.RememberMe });

        ModelState.AddModelError(string.Empty, "شماره موبایل یا رمز عبور نادرست است.");
        return Page();
    }

    /// <summary>
    /// فقط مسیرهای داخلی پذیرفته می‌شوند. بدون این بررسی، مهاجم می‌تواند
    /// با لینک ?returnUrl=https://... کاربر را پس از ورود به سایت دیگری ببرد.
    /// </summary>
    private string? SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;
}