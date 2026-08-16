using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Identity;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Areas.Admin.Pages.Porseshha;

[Authorize(Policy = Permissions.Rulings.Answer)]
public class IndexModel(AppDbContext db) : PageModel
{
    private const int PageSize = 20;

    public IReadOnlyList<UserQuestion> Questions { get; private set; } = [];
    public IReadOnlyList<RulingCategory> Categories { get; private set; } = [];

    [BindProperty(SupportsGet = true)] public QuestionStatus? Status { get; set; } = QuestionStatus.New;
    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true, Name = "safhe")] public int PageNumber { get; set; } = 1;

    public int TotalCount { get; private set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public int NewCount { get; private set; }
    public int AssignedCount { get; private set; }

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Categories = await db.RulingCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ToListAsync(ct);

        NewCount = await db.UserQuestions.CountAsync(q => q.Status == QuestionStatus.New, ct);
        AssignedCount = await db.UserQuestions.CountAsync(q => q.Status == QuestionStatus.Assigned, ct);

        var query = db.UserQuestions.AsNoTracking()
            .Include(q => q.RulingCategory)
            .AsQueryable();

        if (Status is not null) query = query.Where(q => q.Status == Status);

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = Q.Trim();
            query = query.Where(q =>
                q.Body.Contains(term) ||
                q.TrackingCode.Contains(term) ||
                (q.SenderMobile != null && q.SenderMobile.Contains(term)));
        }

        TotalCount = await query.CountAsync(ct);

        if (PageNumber < 1) PageNumber = 1;
        if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

        Questions = await query
            .OrderByDescending(q => q.CreatedAtUtc)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, CancellationToken ct)
    {
        var question = await db.UserQuestions.FirstOrDefaultAsync(q => q.Id == id, ct);
        if (question is null) return NotFound();

        question.Status = QuestionStatus.Rejected;
        await db.SaveChangesAsync(ct);

        Flash = "پرسش به عنوان بررسی‌شده بدون پاسخ علامت خورد.";
        FlashKind = "ok";
        return RedirectToPage(new { Status, Q, safhe = PageNumber });
    }
}