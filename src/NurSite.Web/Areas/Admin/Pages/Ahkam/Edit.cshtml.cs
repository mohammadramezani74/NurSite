using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;
using NurSite.Application.Services;
using NurSite.Web.Services;

namespace NurSite.Web.Areas.Admin.Pages.Ahkam;

public class EditModel(AppDbContext db, ISlugService slugs, ILogger<EditModel> logger) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public SelectList CategoryOptions { get; private set; } = default!;
    public SelectList MarjaOptions { get; private set; } = default!;
    public bool IsNew => Input.Id == 0;
    public string CanonicalBase { get; private set; } = "";

    /// <summary>??? ??? ??? ?? ?? ???? ????? ????? ??????? ??? ???? ????.</summary>
    public UserQuestion? SourceQuestion { get; private set; }

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        /// <summary>????? ???? ?????? ?? ??? ??? ?? ?? ????? ??????.</summary>
        public int? FromQuestionId { get; set; }

        [Required(ErrorMessage = "??? ???? ?? ???????")]
        [StringLength(400, ErrorMessage = "???? ????? ??? ?? ??? ??????? ????")]
        [Display(Name = "????")]
        public string Question { get; set; } = default!;

        [Required(ErrorMessage = "??? ???? ?? ???????")]
        [Display(Name = "????")]
        public string Answer { get; set; } = default!;

        [StringLength(250)]
        [Display(Name = "????? ????")]
        public string? Slug { get; set; }

        [Required(ErrorMessage = "??? ????? ?? ?????? ????")]
        [Display(Name = "???")]
        public int? RulingCategoryId { get; set; }

        [Display(Name = "???? ?????")]
        public int? MarjaId { get; set; }

        [StringLength(250, ErrorMessage = "??? ????? ????? ??? ?? ??? ??????? ????")]
        [Display(Name = "????? ?????? ????")]
        public string? FatwaNote { get; set; }

        [StringLength(400)]
        [Display(Name = "????")]
        public string? SourceReference { get; set; }

        [Display(Name = "?????")]
        public PublishStatus Status { get; set; } = PublishStatus.Draft;

        [Display(Name = "????? ?? ????? ???????")]
        public bool IsFrequentlyAsked { get; set; }

        [Range(0, 999)]
        [Display(Name = "????? ?? ???")]
        public int SortOrder { get; set; }

        [StringLength(70, ErrorMessage = "????? ??? ????? ??? ?? ?? ??????? ????")]
        [Display(Name = "????? ???")]
        public string? MetaTitle { get; set; }

        [StringLength(170, ErrorMessage = "????? ??? ????? ??? ?? ??? ??????? ????")]
        [Display(Name = "????? ???")]
        public string? MetaDescription { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int? id, int? fromQuestion, CancellationToken ct)
    {
        await LoadOptionsAsync(ct);

        // ???? ??? ?? ??? ?? ???? ?????
        if (id is null or 0 && fromQuestion is not null)
        {
            SourceQuestion = await db.UserQuestions.AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == fromQuestion, ct);

            if (SourceQuestion is null) return NotFound();

            Input.FromQuestionId = SourceQuestion.Id;
            Input.Question = Truncate(SourceQuestion.Body, 400);
            Input.Answer = SourceQuestion.AnswerBody ?? string.Empty;
            Input.RulingCategoryId = SourceQuestion.RulingCategoryId;
            return Page();
        }

        if (id is null or 0) return Page();

        var ruling = await db.Rulings.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (ruling is null) return NotFound();

        Input = new InputModel
        {
            Id = ruling.Id,
            Question = ruling.Question,
            Answer = ruling.Answer,
            Slug = ruling.Slug,
            RulingCategoryId = ruling.RulingCategoryId,
            MarjaId = ruling.MarjaId,
            FatwaNote = ruling.FatwaNote,
            SourceReference = ruling.SourceReference,
            Status = ruling.Status,
            IsFrequentlyAsked = ruling.IsFrequentlyAsked,
            SortOrder = ruling.SortOrder,
            MetaTitle = ruling.MetaTitle,
            MetaDescription = ruling.MetaDescription
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadOptionsAsync(ct);

        // ???? ???? ?? ????? ????? ????. ???? ?? ?? ??? ????? ???? ????
        // ???? ????? ????? ???? ??? ??????? ???? ????.
        if (Input.Status == PublishStatus.Published &&
            Input.MarjaId is null &&
            string.IsNullOrWhiteSpace(Input.FatwaNote))
        {
            ModelState.AddModelError("Input.MarjaId",
                "???? ??????? ???? ????? ?? ????? ?????? ???? ?? ???? ????.");
        }

        if (!ModelState.IsValid) return Page();

        var isNew = Input.Id == 0;
        var ruling = isNew
            ? new Ruling()
            : await db.Rulings.FirstOrDefaultAsync(r => r.Id == Input.Id, ct);

        if (ruling is null) return NotFound();

        var previousSlug = ruling.Slug;

        var desiredSlug = string.IsNullOrWhiteSpace(Input.Slug) ? Input.Question : Input.Slug;
        ruling.Slug = await slugs.GenerateUniqueAsync<Ruling>(
            desiredSlug, isNew ? null : ruling.Id, ct);

        ruling.Question = Input.Question.Trim();
        ruling.Answer = Input.Answer.Trim();
        ruling.RulingCategoryId = Input.RulingCategoryId!.Value;
        ruling.MarjaId = Input.MarjaId;
        ruling.FatwaNote = Input.FatwaNote?.Trim();
        ruling.SourceReference = Input.SourceReference?.Trim();
        ruling.Status = Input.Status;
        ruling.IsFrequentlyAsked = Input.IsFrequentlyAsked;
        ruling.SortOrder = Input.SortOrder;

        // ???? ?? ??? ?????? ??? ????? ??????? ????? ???? ?? ????? ??????
        ruling.SearchText = PersianText.Normalize(
            $"{ruling.Question} {ruling.Question} {ruling.Answer} {ruling.FatwaNote}");

        // ??? ???? ???? ?????? ????? ??? ???? ??? ????? ??????
        // ???? ?? ?? ???? ????? ??????
        ruling.MetaTitle = string.IsNullOrWhiteSpace(Input.MetaTitle)
            ? Truncate(ruling.Question, 70)
            : Input.MetaTitle.Trim();

        ruling.MetaDescription = string.IsNullOrWhiteSpace(Input.MetaDescription)
            ? Truncate(ReadingTime.Excerpt(ruling.Answer, 170), 170)
            : Input.MetaDescription.Trim();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (isNew)
        {
            ruling.CreatedById = userId;
            db.Rulings.Add(ruling);
        }
        else
        {
            ruling.UpdatedById = userId;
        }

        await db.SaveChangesAsync(ct);

        // ????? ???? ????? ?? ??? ????????
        if (Input.FromQuestionId is not null)
        {
            var question = await db.UserQuestions
                .FirstOrDefaultAsync(q => q.Id == Input.FromQuestionId, ct);

            if (question is not null)
            {
                question.PublishedRulingId = ruling.Id;
                question.Status = QuestionStatus.Published;
                await db.SaveChangesAsync(ct);
            }
        }

        if (!isNew && !string.IsNullOrEmpty(previousSlug) && previousSlug != ruling.Slug)
        {
            await AddRedirectAsync(previousSlug, ruling.Slug, ct);
            logger.LogInformation("???????? ??? ?? {Old} ?? {New}", previousSlug, ruling.Slug);
        }

        Flash = isNew ? "??? ??? ??." : "??????? ????? ??.";
        FlashKind = "ok";
        return RedirectToPage("./Edit", new { id = ruling.Id });
    }

    private async Task AddRedirectAsync(string oldSlug, string newSlug, CancellationToken ct)
    {
        var from = $"/ahkam/{oldSlug}";
        var to = $"/ahkam/{newSlug}";

        var existing = await db.UrlRedirects.FirstOrDefaultAsync(r => r.FromPath == from, ct);
        if (existing is not null)
        {
            existing.ToPath = to;
            existing.IsActive = true;
        }
        else
        {
            db.UrlRedirects.Add(new UrlRedirect
            {
                FromPath = from,
                ToPath = to,
                StatusCode = 301,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        var chained = await db.UrlRedirects.Where(r => r.ToPath == from).ToListAsync(ct);
        foreach (var r in chained) r.ToPath = to;

        await db.SaveChangesAsync(ct);
    }

    private async Task LoadOptionsAsync(CancellationToken ct)
    {
        var categories = await db.RulingCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Title).ToListAsync(ct);
        CategoryOptions = new SelectList(categories, nameof(RulingCategory.Id), nameof(RulingCategory.Title));

        var marjas = await db.Marjas.AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder).ThenBy(m => m.FullName).ToListAsync(ct);
        MarjaOptions = new SelectList(marjas, nameof(Marja.Id), nameof(Marja.FullName));

        var siteSetting = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        CanonicalBase = (siteSetting?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        value = value.Trim();
        if (value.Length <= max) return value;

        var cut = value[..(max - 1)].TrimEnd();
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > max / 2) cut = cut[..lastSpace];

        return cut + "…";
    }
}