using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NurSite.Infrastructure.Identity;

namespace NurSite.Web.Pages;

[Authorize]
public class KhoroojModel(
    SignInManager<ApplicationUser> signInManager,
    ILogger<KhoroojModel> logger) : PageModel
{
    /// <summary>
    /// اگر کاربر مستقیم آدرس را باز کند، صفحه تأیید نمایش داده می‌شود.
    /// خروج فقط با POST انجام می‌شود تا با یک لینک ساده یا پیش‌واکشی مرورگر
    /// نتوان کاربر را ناخواسته خارج کرد.
    /// </summary>
    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        await signInManager.SignOutAsync();
        logger.LogInformation("خروج کاربر {UserId}", userId);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToPage("/Index");
    }
}