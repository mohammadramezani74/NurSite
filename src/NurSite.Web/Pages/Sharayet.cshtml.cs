using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NurSite.Web.Pages;

public class SharayetModel : PageModel
{
    /// <summary>تاریخ آخرین بازنگری متن، دستی نگه‌داری می‌شود.</summary>
    public DateTime UpdatedAtUtc { get; } = new(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc);
}