using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Services;
using NurSite.Infrastructure.Identity;
using NurSite.Web.Helpers;

namespace NurSite.Web.Areas.Admin.Pages.Karbaran;

/// <summary>
/// فهرست کاربران و ساخت کاربر تازه.
///
/// «ساخت کاربر» یعنی ثبت پیشاپیش یک شماره و تعیین نقشش. رمز عبوری در
/// کار نیست چون ورود فقط با کد پیامکی است؛ کاربر هر وقت با همان شماره
/// وارد شد، حسابش با نقش تعیین‌شده آماده است.
/// </summary>
[Authorize(Policy = Permissions.Users.Manage)]
public class IndexModel(
    UserManager<ApplicationUser> users,
    RoleManager<ApplicationRole> roles,
    ILogger<IndexModel> logger) : PageModel
{
    private const int PageSize = 20;

    public sealed record Row(ApplicationUser User, IReadOnlyList<string> Roles);

    public IReadOnlyList<Row> Rows { get; private set; } = [];
    public IReadOnlyList<ApplicationRole> AllRoles { get; private set; } = [];

    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true)] public string? Role { get; set; }
    [BindProperty(SupportsGet = true)] public bool? Active { get; set; }

    // نام «page» رزرو شده است و مسیر خود صفحه را حمل می‌کند
    [BindProperty(SupportsGet = true, Name = "safhe")] public int PageNumber { get; set; } = 1;

    public int TotalCount { get; private set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<int?> PagerPages => Pager.Pages(PageNumber, TotalPages);

    [BindProperty] public InputModel Input { get; set; } = new();

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "شماره موبایل را وارد کنید")]
        [Display(Name = "شماره موبایل")]
        public string Mobile { get; set; } = default!;

        [StringLength(150)]
        [Display(Name = "نام و نام خانوادگی")]
        public string? FullName { get; set; }

        [Display(Name = "نقش")]
        public string RoleName { get; set; } = AppRoles.Member;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken ct)
    {
        var mobile = MobileNumber.Normalize(Input.Mobile);
        if (mobile is null)
            ModelState.AddModelError("Input.Mobile", "شماره موبایل معتبر نیست.");
        else if (await users.FindByNameAsync(mobile) is not null)
            ModelState.AddModelError("Input.Mobile", "این شماره از قبل ثبت شده است.");

        if (!await roles.RoleExistsAsync(Input.RoleName))
            ModelState.AddModelError("Input.RoleName", "نقش انتخابی معتبر نیست.");

        if (!ModelState.IsValid)
        {
            await LoadAsync(ct);
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = mobile,
            PhoneNumber = mobile,
            // شماره را مدیر وارد کرده، نه خود کاربر؛ تأییدش وقتی است
            // که کاربر با کد پیامکی وارد شود
            PhoneNumberConfirmed = false,
            FullName = string.IsNullOrWhiteSpace(Input.FullName) ? null : Input.FullName.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        var created = await users.CreateAsync(user);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            await LoadAsync(ct);
            return Page();
        }

        await users.AddToRoleAsync(user, Input.RoleName);
        logger.LogInformation("کاربر {UserId} توسط مدیر ساخته شد", user.Id);

        Flash = $"کاربر «{user.FullName ?? mobile!}» ساخته شد. با همین شماره می‌تواند وارد شود.";
        FlashKind = "ok";
        return RedirectToCurrent();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(string id, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (await IsLastSuperAdminAsync(user) && user.IsActive)
        {
            Flash = "این تنها مدیر ارشد فعال است و نمی‌شود غیرفعالش کرد.";
            FlashKind = "warn";
            return RedirectToCurrent();
        }

        if (user.Id == users.GetUserId(User) && user.IsActive)
        {
            Flash = "نمی‌توانید حساب خودتان را غیرفعال کنید.";
            FlashKind = "warn";
            return RedirectToCurrent();
        }

        user.IsActive = !user.IsActive;
        await users.UpdateAsync(user);

        // با تغییر مهر امنیتی، نشست‌های باز آن کاربر باطل می‌شوند؛
        // وگرنه کاربر غیرفعال تا دو هفته با کوکی قبلی داخل می‌ماند
        await users.UpdateSecurityStampAsync(user);

        Flash = user.IsActive ? "حساب فعال شد." : "حساب غیرفعال شد.";
        FlashKind = "ok";
        return RedirectToCurrent();
    }

    private async Task<bool> IsLastSuperAdminAsync(ApplicationUser user)
    {
        if (!await users.IsInRoleAsync(user, AppRoles.SuperAdmin)) return false;

        var admins = await users.GetUsersInRoleAsync(AppRoles.SuperAdmin);
        return admins.Count(a => a.IsActive) <= 1;
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        AllRoles = await roles.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct);

        var query = users.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = Q.Trim();
            var asMobile = MobileNumber.Normalize(term);

            query = asMobile is not null
                ? query.Where(u => u.UserName == asMobile)
                : query.Where(u => u.FullName != null && u.FullName.Contains(term));
        }

        if (Active is not null) query = query.Where(u => u.IsActive == Active);

        if (!string.IsNullOrWhiteSpace(Role))
        {
            // فیلتر نقش با کوئری روی جدول‌های Identity ممکن نیست مگر با
            // پیوند دستی؛ ساده‌تر آن است که شناسه‌ها را جدا بگیریم
            var inRole = await users.GetUsersInRoleAsync(Role);
            var ids = inRole.Select(u => u.Id).ToList();
            query = query.Where(u => ids.Contains(u.Id));
        }

        TotalCount = await query.CountAsync(ct);

        if (PageNumber < 1) PageNumber = 1;
        if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

        var page = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);

        var rows = new List<Row>(page.Count);
        foreach (var user in page)
            rows.Add(new Row(user, (await users.GetRolesAsync(user)).ToList()));

        Rows = rows;
    }

    public string RoleDisplay(string name) =>
        AllRoles.FirstOrDefault(r => r.Name == name)?.DisplayName ?? name;

    private IActionResult RedirectToCurrent() =>
        RedirectToPage(new { Q, Role, Active, safhe = PageNumber });
}