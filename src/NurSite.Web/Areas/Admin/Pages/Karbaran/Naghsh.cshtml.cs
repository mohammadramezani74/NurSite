using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Infrastructure.Identity;

namespace NurSite.Web.Areas.Admin.Pages.Karbaran;

/// <summary>
/// ویرایش یک کاربر: نام، نقش‌ها، و دیدن دسترسی‌هایی که از آن نقش‌ها می‌گیرد.
/// </summary>
[Authorize(Policy = Permissions.Users.Manage)]
public class NaghshModel(
    UserManager<ApplicationUser> users,
    RoleManager<ApplicationRole> roles,
    ILogger<NaghshModel> logger) : PageModel
{
    public ApplicationUser Target { get; private set; } = default!;
    public IReadOnlyList<ApplicationRole> AllRoles { get; private set; } = [];

    /// <summary>دسترسی‌هایی که مجموع نقش‌های فعلی به کاربر می‌دهد.</summary>
    public IReadOnlyList<string> EffectivePermissions { get; private set; } = [];

    public bool IsSelf { get; private set; }

    [BindProperty] public InputModel Input { get; set; } = new();

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class InputModel
    {
        public string Id { get; set; } = default!;

        [StringLength(150)]
        [Display(Name = "نام و نام خانوادگی")]
        public string? FullName { get; set; }

        [Display(Name = "نقش‌ها")]
        public List<string> RoleNames { get; set; } = [];
    }

    public async Task<IActionResult> OnGetAsync(string id, CancellationToken ct)
    {
        if (!await LoadAsync(id, ct)) return NotFound();

        Input = new InputModel
        {
            Id = Target.Id,
            FullName = Target.FullName,
            RoleNames = (await users.GetRolesAsync(Target)).ToList()
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!await LoadAsync(Input.Id, ct)) return NotFound();

        var current = (await users.GetRolesAsync(Target)).ToList();
        var wanted = Input.RoleNames.Where(r => AllRoles.Any(a => a.Name == r)).Distinct().ToList();

        // اگر آخرین مدیر ارشد فعال، مدیریت ارشدش را از دست بدهد، دیگر
        // هیچ‌کس نمی‌تواند نقش‌ها را عوض کند — و راه برگشتی هم نیست
        if (current.Contains(AppRoles.SuperAdmin) && !wanted.Contains(AppRoles.SuperAdmin))
        {
            var admins = await users.GetUsersInRoleAsync(AppRoles.SuperAdmin);
            if (admins.Count(a => a.IsActive) <= 1)
            {
                Flash = "این تنها مدیر ارشد فعال است؛ نقش مدیر ارشد را نمی‌شود از او گرفت.";
                FlashKind = "warn";
                return RedirectToPage(new { id = Input.Id });
            }
        }

        if (!ModelState.IsValid) return Page();

        Target.FullName = string.IsNullOrWhiteSpace(Input.FullName) ? null : Input.FullName.Trim();
        await users.UpdateAsync(Target);

        var toRemove = current.Except(wanted).ToList();
        var toAdd = wanted.Except(current).ToList();

        if (toRemove.Count > 0) await users.RemoveFromRolesAsync(Target, toRemove);
        if (toAdd.Count > 0) await users.AddToRolesAsync(Target, toAdd);

        if (toRemove.Count > 0 || toAdd.Count > 0)
        {
            // دسترسی‌ها در کوکی کاربر نشسته‌اند؛ بدون تازه‌کردن مهر امنیتی
            // تغییر نقش تا ۲۴ ساعت اثر نمی‌کند
            await users.UpdateSecurityStampAsync(Target);

            logger.LogInformation("نقش‌های کاربر {UserId} عوض شد. افزوده {Added}، برداشته {Removed}",
                Target.Id, string.Join(",", toAdd), string.Join(",", toRemove));
        }

        Flash = "تغییرات ذخیره شد.";
        FlashKind = "ok";
        return RedirectToPage(new { id = Target.Id });
    }

    private async Task<bool> LoadAsync(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;

        var user = await users.FindByIdAsync(id);
        if (user is null) return false;

        Target = user;
        IsSelf = user.Id == users.GetUserId(User);

        AllRoles = await roles.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct);

        var granted = new HashSet<string>();
        foreach (var name in await users.GetRolesAsync(user))
        {
            var role = await roles.FindByNameAsync(name);
            if (role is null) continue;

            foreach (var claim in await roles.GetClaimsAsync(role))
                if (claim.Type == Permissions.ClaimType) granted.Add(claim.Value);
        }

        EffectivePermissions = granted.OrderBy(p => p).ToList();
        return true;
    }

    /// <summary>نام فارسی دسترسی، برای نمایش. کلیدهای ناشناخته خام می‌مانند.</summary>
    public static string PermissionDisplay(string permission) => permission switch
    {
        "articles.view" => "دیدن مقالات",
        "articles.create" => "نوشتن مقاله",
        "articles.edit" => "ویرایش مقاله",
        "articles.delete" => "حذف مقاله",
        "articles.publish" => "انتشار مقاله",
        "rulings.view" => "دیدن احکام",
        "rulings.answer" => "پاسخ به پرسش شرعی",
        "rulings.publish" => "انتشار حکم",
        "media.manage" => "مدیریت رسانه",
        "events.manage" => "مدیریت برنامه‌ها",
        "users.manage" => "مدیریت کاربران",
        "settings.manage" => "تنظیمات سایت",
        _ => permission
    };
}