using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Infrastructure.Identity;

namespace NurSite.Web.Areas.Admin.Pages.Karbaran;

/// <summary>
/// دسترسی هر نقش. اینجا تعیین می‌شود که یک نقش به کدام بخش‌های پنل راه دارد.
///
/// دسترسی‌ها روی نقش می‌نشینند نه روی کاربر، پس تغییر اینجا برای همه
/// کسانی که آن نقش را دارند اعمال می‌شود.
/// </summary>
[Authorize(Policy = Permissions.Users.Manage)]
public class NaghshhaModel(
    RoleManager<ApplicationRole> roles,
    UserManager<ApplicationUser> users,
    ILogger<NaghshhaModel> logger) : PageModel
{
    public sealed record RoleRow(
        ApplicationRole Role,
        int UserCount,
        IReadOnlyList<string> Permissions);

    public IReadOnlyList<RoleRow> Rows { get; private set; } = [];

    /// <summary>نقشی که در حال ویرایش است.</summary>
    public ApplicationRole? Editing { get; private set; }

    [BindProperty] public string? RoleName { get; set; }
    [BindProperty] public List<string> Selected { get; set; } = [];

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    /// <summary>دسترسی‌ها گروه‌بندی‌شده، به همان ترتیبی که تعریف شده‌اند.</summary>
    public static IReadOnlyList<IGrouping<string, string>> Groups { get; } =
        Permissions.All()
            .GroupBy(p => Permissions.Describe.TryGetValue(p, out var d) ? d.Group : "سایر")
            .ToList();

    public async Task<IActionResult> OnGetAsync(string? edit, CancellationToken ct)
    {
        await LoadAsync(ct);

        if (string.IsNullOrWhiteSpace(edit)) return Page();

        Editing = await roles.FindByNameAsync(edit);
        if (Editing is null) return NotFound();

        RoleName = Editing.Name;
        Selected = Rows.First(r => r.Role.Id == Editing.Id).Permissions.ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(RoleName)) return NotFound();

        var role = await roles.FindByNameAsync(RoleName);
        if (role is null) return NotFound();

        var wanted = Selected.Where(p => Permissions.All().Contains(p)).Distinct().ToList();

        // مدیر ارشد باید همیشه همه دسترسی‌ها را داشته باشد. اگر کسی
        // مدیریت کاربران را از این نقش بردارد، دیگر هیچ‌کس نمی‌تواند
        // دسترسی‌ها را برگرداند و پنل برای همیشه قفل می‌شود.
        if (role.Name == AppRoles.SuperAdmin && !wanted.Contains(Permissions.Users.Manage))
        {
            Flash = "مدیر ارشد باید مدیریت کاربران را داشته باشد، وگرنه راه بازگشتی نمی‌ماند.";
            FlashKind = "warn";
            return RedirectToPage(new { edit = role.Name });
        }

        var current = (await roles.GetClaimsAsync(role))
            .Where(c => c.Type == Permissions.ClaimType)
            .ToList();

        foreach (var claim in current.Where(c => !wanted.Contains(c.Value)))
            await roles.RemoveClaimAsync(role, claim);

        foreach (var permission in wanted.Where(p => current.All(c => c.Value != p)))
            await roles.AddClaimAsync(role,
                new System.Security.Claims.Claim(Permissions.ClaimType, permission));

        // دسترسی‌ها در کوکی کاربران نشسته‌اند. بدون تازه‌کردن مهر امنیتی،
        // تغییر تا ۲۴ ساعت روی کسانی که همین حالا واردند اثر نمی‌کند.
        var affected = await users.GetUsersInRoleAsync(role.Name!);
        foreach (var user in affected)
            await users.UpdateSecurityStampAsync(user);

        logger.LogInformation("دسترسی‌های نقش {Role} عوض شد؛ {Count} کاربر تحت تأثیر", role.Name, affected.Count);

        Flash = affected.Count > 0
            ? $"دسترسی‌های «{role.DisplayName}» ذخیره شد. {affected.Count} کاربر باید دوباره وارد شوند."
            : $"دسترسی‌های «{role.DisplayName}» ذخیره شد.";
        FlashKind = "ok";

        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var all = await roles.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct);

        var rows = new List<RoleRow>(all.Count);
        foreach (var role in all)
        {
            var tracked = await roles.FindByNameAsync(role.Name!);
            var claims = tracked is null
                ? []
                : (await roles.GetClaimsAsync(tracked))
                    .Where(c => c.Type == Permissions.ClaimType)
                    .Select(c => c.Value)
                    .ToList();

            var count = (await users.GetUsersInRoleAsync(role.Name!)).Count;
            rows.Add(new RoleRow(role, count, claims));
        }

        Rows = rows;
    }

    public static string Title(string permission) =>
        Permissions.Describe.TryGetValue(permission, out var d) ? d.Title : permission;

    public static string Hint(string permission) =>
        Permissions.Describe.TryGetValue(permission, out var d) ? d.Hint : "";
}