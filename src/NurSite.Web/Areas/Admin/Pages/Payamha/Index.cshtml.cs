using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Entities;
using NurSite.Infrastructure.Identity;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Areas.Admin.Pages.Payamha;

[Authorize(Policy = Permissions.Settings.Manage)]
public class IndexModel(AppDbContext db) : PageModel
{
    private const int PageSize = 20;

    public IReadOnlyList<ContactMessage> Messages { get; private set; } = [];

    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true, Name = "unread")] public bool? UnreadOnly { get; set; }
    [BindProperty(SupportsGet = true, Name = "safhe")] public int PageNumber { get; set; } = 1;

    public int TotalCount { get; private set; }
    public int UnreadCount { get; private set; }
    public int TodayCount { get; private set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>پیامی که در پنل کناری باز است.</summary>
    public ContactMessage? Selected { get; private set; }

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public async Task OnGetAsync(int? open, CancellationToken ct)
    {
        UnreadCount = await db.ContactMessages.CountAsync(m => !m.IsRead, ct);

        var since = DateTime.UtcNow.AddDays(-1);
        TodayCount = await db.ContactMessages.CountAsync(m => m.CreatedAtUtc >= since, ct);

        var query = db.ContactMessages.AsNoTracking().AsQueryable();

        if (UnreadOnly == true) query = query.Where(m => !m.IsRead);

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = Q.Trim();
            query = query.Where(m =>
                m.SenderName.Contains(term) ||
                m.Body.Contains(term) ||
                (m.Subject != null && m.Subject.Contains(term)) ||
                (m.SenderMobile != null && m.SenderMobile.Contains(term)));
        }

        TotalCount = await query.CountAsync(ct);

        if (PageNumber < 1) PageNumber = 1;
        if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

        Messages = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);

        // باز کردن پیام، و علامت‌گذاری خوانده‌شده در همان لحظه
        if (open is not null)
        {
            var message = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == open, ct);
            if (message is not null)
            {
                if (!message.IsRead)
                {
                    message.IsRead = true;
                    await db.SaveChangesAsync(ct);
                    UnreadCount = Math.Max(0, UnreadCount - 1);
                }
                Selected = message;
            }
        }
    }

    public async Task<IActionResult> OnPostToggleReadAsync(int id, CancellationToken ct)
    {
        var message = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (message is null) return NotFound();

        message.IsRead = !message.IsRead;
        await db.SaveChangesAsync(ct);

        Flash = message.IsRead ? "خوانده‌شده علامت خورد." : "به خوانده‌نشده برگشت.";
        FlashKind = "ok";
        return RedirectToPage(new { Q, unread = UnreadOnly, safhe = PageNumber });
    }

    public async Task<IActionResult> OnPostNoteAsync(int id, string? note, CancellationToken ct)
    {
        var message = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (message is null) return NotFound();

        message.AdminNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        await db.SaveChangesAsync(ct);

        Flash = "یادداشت ذخیره شد.";
        FlashKind = "ok";
        return RedirectToPage(new { open = id, Q, unread = UnreadOnly, safhe = PageNumber });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var message = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (message is null) return NotFound();

        db.ContactMessages.Remove(message);
        await db.SaveChangesAsync(ct);

        Flash = "پیام حذف شد.";
        FlashKind = "ok";
        return RedirectToPage(new { Q, unread = UnreadOnly, safhe = PageNumber });
    }

    /// <summary>همه پیام‌های خوانده‌نشده را یکجا علامت می‌زند.</summary>
    public async Task<IActionResult> OnPostMarkAllReadAsync(CancellationToken ct)
    {
        var count = await db.ContactMessages
            .Where(m => !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true), ct);

        Flash = $"{count} پیام خوانده‌شده علامت خورد.";
        FlashKind = "ok";
        return RedirectToPage();
    }
}