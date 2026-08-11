using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Services;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Pages;

[AllowAnonymous]
public class PeygiriModel(AppDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "code")]
    [Display(Name = "کد رهگیری")]
    public string? Code { get; set; }

    public UserQuestion? Question { get; private set; }
    public string? PublishedSlug { get; private set; }
    public bool Searched { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Code)) return;

        Searched = true;
        var normalized = TrackingCode.Normalize(Code);

        Question = await db.UserQuestions.AsNoTracking()
            .Include(q => q.RulingCategory)
            .FirstOrDefaultAsync(q => q.TrackingCode == normalized, ct);

        // اگر پاسخ در آرشیو عمومی منتشر شده، نشانی‌اش را هم بدهیم
        if (Question?.PublishedRulingId is not null)
        {
            PublishedSlug = await db.Rulings.AsNoTracking()
                .Where(r => r.Id == Question.PublishedRulingId && r.Status == PublishStatus.Published)
                .Select(r => r.Slug)
                .FirstOrDefaultAsync(ct);
        }
    }

    public static string StatusLabel(QuestionStatus status) => status switch
    {
        QuestionStatus.New => "در نوبت بررسی",
        QuestionStatus.Assigned => "ارجاع‌شده به پاسخگو",
        QuestionStatus.Answered => "پاسخ داده شده",
        QuestionStatus.Published => "پاسخ داده شده و در آرشیو منتشر شده",
        QuestionStatus.Rejected => "بررسی شد اما پاسخی ثبت نشد",
        _ => "نامشخص"
    };
}