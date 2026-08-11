using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Areas.Admin.Pages.Porseshha;

public class AnswerModel(
    AppDbContext db,
    INotificationService notifications,
    ILogger<AnswerModel> logger) : PageModel
{
    public UserQuestion Question { get; private set; } = default!;
    public SelectList CategoryOptions { get; private set; } = default!;
    public string? PublishedSlug { get; private set; }

    [BindProperty] public InputModel Input { get; set; } = new();

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "متن پاسخ را بنویسید")]
        [Display(Name = "پاسخ")]
        public string AnswerBody { get; set; } = default!;

        [Display(Name = "باب")]
        public int? RulingCategoryId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        if (!await LoadAsync(id, ct)) return NotFound();

        Input = new InputModel
        {
            Id = Question.Id,
            AnswerBody = Question.AnswerBody ?? string.Empty,
            RulingCategoryId = Question.RulingCategoryId
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, CancellationToken ct)
    {
        if (!await LoadAsync(id, ct)) return NotFound();
        if (!ModelState.IsValid) return Page();

        var question = await db.UserQuestions.FirstOrDefaultAsync(q => q.Id == id, ct);
        if (question is null) return NotFound();

        var isFirstAnswer = string.IsNullOrWhiteSpace(question.AnswerBody);

        question.AnswerBody = Input.AnswerBody.Trim();
        question.RulingCategoryId = Input.RulingCategoryId;
        question.AnsweredAtUtc = DateTime.UtcNow;

        // اگر قبلاً در آرشیو منتشر شده، وضعیتش را عقب نمی‌بریم
        if (question.Status != QuestionStatus.Published)
            question.Status = QuestionStatus.Answered;

        await db.SaveChangesAsync(ct);

        // اطلاع‌رسانی فقط بار اول، نه در هر ویرایش
        if (isFirstAnswer)
        {
            await notifications.AnswerReadyAsync(
                new NotificationTarget(question.SenderMobile, question.SenderEmail, question.SenderName),
                question.TrackingCode, ct);

            logger.LogInformation("پاسخ پرسش {Code} ثبت شد.", question.TrackingCode);
        }

        Flash = "پاسخ ذخیره شد.";
        FlashKind = "ok";
        return RedirectToPage("./Answer", new { id });
    }

    /// <summary>ارجاع پرسش به خود کاربر جاری، تا مشخص باشد چه کسی رویش کار می‌کند.</summary>
    public async Task<IActionResult> OnPostAssignAsync(int id, CancellationToken ct)
    {
        var question = await db.UserQuestions.FirstOrDefaultAsync(q => q.Id == id, ct);
        if (question is null) return NotFound();

        question.AssignedToUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (question.Status == QuestionStatus.New)
            question.Status = QuestionStatus.Assigned;

        await db.SaveChangesAsync(ct);

        Flash = "پرسش به شما ارجاع شد.";
        FlashKind = "ok";
        return RedirectToPage("./Answer", new { id });
    }

    private async Task<bool> LoadAsync(int id, CancellationToken ct)
    {
        var question = await db.UserQuestions.AsNoTracking()
            .Include(q => q.RulingCategory)
            .FirstOrDefaultAsync(q => q.Id == id, ct);

        if (question is null) return false;
        Question = question;

        var categories = await db.RulingCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
            .ToListAsync(ct);
        CategoryOptions = new SelectList(categories, nameof(RulingCategory.Id), nameof(RulingCategory.Title));

        if (question.PublishedRulingId is not null)
        {
            PublishedSlug = await db.Rulings.AsNoTracking()
                .Where(r => r.Id == question.PublishedRulingId)
                .Select(r => r.Slug)
                .FirstOrDefaultAsync(ct);
        }

        return true;
    }
}