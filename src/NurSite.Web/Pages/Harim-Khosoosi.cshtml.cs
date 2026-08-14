using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NurSite.Web.Pages;

public class HarimKhosoosiModel : PageModel
{
    /// <summary>
    /// تاریخ آخرین بازنگری متن. دستی نگه‌داری می‌شود چون به تغییر فایل
    /// ربطی ندارد — یک اصلاح نگارشی، بازنگری در سیاست نیست.
    /// </summary>
    public DateTime UpdatedAtUtc { get; } = new(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc);
}