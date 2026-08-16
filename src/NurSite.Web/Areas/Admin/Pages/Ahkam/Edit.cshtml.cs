using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Identity;
using NurSite.Infrastructure.Persistence;
using NurSite.Application.Services;
using NurSite.Web.Services;

namespace NurSite.Web.Areas.Admin.Pages.Ahkam;

[Authorize(Policy = Permissions.Rulings.Answer)]
public class EditModel(AppDbContext db, ISlugService slugs, ILogger<EditModel> logger) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public SelectList CategoryOptions { get; private set; } = default!;
    public SelectList MarjaOptions { get; private set; } = default!;
    public bool IsNew => Input.Id == 0;
    public string CanonicalBase { get; private set; } = "";

    /// <summary>
    /// این حکم واقعاً گره نمودار دارد؟ فرق دارد با Input.HasDiagram که
    /// فقط «قصد» کاربر است. تا وقتی گره ساخته نشده، کاربر باید بتواند
    /// تیک نموداری بودن را بردارد؛ بعد از آن نه.
    /// </summary>
    public bool HasDiagramNodes { get; private set; }

    /// <summary>اگر این حکم از یک پرسش کاربر ساخته می‌شود، متن اصلی پرسش.</summary>
    public UserQuestion? SourceQuestion { get; private set; }

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        /// <summary>شناسه پرسش کاربری که این حکم از آن ساخته می‌شود.</summary>
        public int? FromQuestionId { get; set; }

        [Required(ErrorMessage = "متن پرسش را بنویسید")]
        [StringLength(400, ErrorMessage = "پرسش نباید بیش از ۴۰۰ کاراکتر باشد")]
        [Display(Name = "پرسش")]
        public string Question { get; set; } = default!;

        /// <summary>
        /// در احکام نموداری خالی می‌ماند و محتوا در نمودار است،
        /// پس اجباری بودنش در کد بررسی می‌شود نه با صفت.
        /// </summary>
        [Display(Name = "پاسخ")]
        public string? Answer { get; set; }

        /// <summary>
        /// محتوای این حکم نموداری است. در فرم یک تیک است، نه فیلد پنهان —
        /// وگرنه در «حکم تازه» هیچ راهی نبود که کاربر بگوید پاسخ متنی ندارد
        /// و می‌خواهد نمودار بسازد، و ذخیره همیشه شکست می‌خورد.
        /// </summary>
        [Display(Name = "حکم نموداری")]
        public bool HasDiagram { get; set; }

        [StringLength(250)]
        [Display(Name = "نشانی صفحه")]
        public string? Slug { get; set; }

        [Required(ErrorMessage = "باب احکام را انتخاب کنید")]
        [Display(Name = "باب")]
        public int? RulingCategoryId { get; set; }

        [Display(Name = "مرجع تقلید")]
        public int? MarjaId { get; set; }

        [StringLength(250, ErrorMessage = "این عبارت نباید بیش از ۲۵۰ کاراکتر باشد")]
        [Display(Name = "عبارت استناد فتوا")]
        public string? FatwaNote { get; set; }

        [StringLength(400)]
        [Display(Name = "منبع")]
        public string? SourceReference { get; set; }

        [Display(Name = "وضعیت")]
        public PublishStatus Status { get; set; } = PublishStatus.Draft;

        [Display(Name = "نمایش در احکام پرتکرار")]
        public bool IsFrequentlyAsked { get; set; }

        [Range(0, 999)]
        [Display(Name = "ترتیب در باب")]
        public int SortOrder { get; set; }

        [StringLength(70, ErrorMessage = "عنوان متا نباید بیش از ۷۰ کاراکتر باشد")]
        [Display(Name = "عنوان متا")]
        public string? MetaTitle { get; set; }

        [StringLength(170, ErrorMessage = "توضیح متا نباید بیش از ۱۷۰ کاراکتر باشد")]
        [Display(Name = "توضیح متا")]
        public string? MetaDescription { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int? id, int? fromQuestion, CancellationToken ct)
    {
        await LoadOptionsAsync(ct);

        // ساخت حکم از روی یک پرسش کاربر
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

        HasDiagramNodes = await db.RulingNodes.AnyAsync(n => n.RulingId == ruling.Id, ct);

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
            HasDiagram = ruling.HasDiagram,
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

        // اگر گره‌ای در دیتابیس هست، حکم نموداری است — هرچه در فرم آمده باشد.
        // بدون این، برداشتن تیک باعث می‌شد صفحه عمومی به‌جای نمودار،
        // پاسخِ خالی را نشان بدهد در حالی که محتوا سر جایش است.
        if (Input.Id != 0)
        {
            HasDiagramNodes = await db.RulingNodes.AnyAsync(n => n.RulingId == Input.Id, ct);
            if (HasDiagramNodes) Input.HasDiagram = true;
        }

        // پاسخ متنی فقط وقتی اجباری است که حکم نموداری نباشد
        if (!Input.HasDiagram && string.IsNullOrWhiteSpace(Input.Answer))
            ModelState.AddModelError("Input.Answer",
                "متن پاسخ را بنویسید، یا اگر محتوای این حکم نموداری است تیک «حکم نموداری» را بزنید.");

        // فتوا باید به منبعی مستند باشد. حکمی که به هیچ مرجعی نسبت داده
        // نشده نباید منتشر شود، چون مسئولیت شرعی دارد.
        // در احکام نموداری، مراجع در سطح شاخه‌ها مشخص می‌شوند، پس
        // منبع کتابی هم استناد کافی محسوب می‌شود
        if (Input.Status == PublishStatus.Published &&
            Input.MarjaId is null &&
            string.IsNullOrWhiteSpace(Input.FatwaNote) &&
            !Input.HasDiagram)
        {
            ModelState.AddModelError("Input.MarjaId",
                "برای انتشار، مرجع تقلید یا عبارت استناد فتوا را مشخص کنید.");
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
        ruling.Answer = Input.Answer?.Trim() ?? string.Empty;
        ruling.RulingCategoryId = Input.RulingCategoryId!.Value;
        ruling.MarjaId = Input.MarjaId;
        ruling.FatwaNote = Input.FatwaNote?.Trim();
        ruling.SourceReference = Input.SourceReference?.Trim();
        ruling.Status = Input.Status;
        ruling.HasDiagram = Input.HasDiagram;
        ruling.IsFrequentlyAsked = Input.IsFrequentlyAsked;
        ruling.SortOrder = Input.SortOrder;

        // پرسش دو بار می‌آید چون کاربر معمولاً عبارت پرسش را جستجو می‌کند.
        // در احکام نموداری، متن شرط‌ها و حکم‌ها هم باید بیاید وگرنه
        // حکم اصلاً در جستجو پیدا نمی‌شود چون Answer خالی است.
        var diagramText = await BuildDiagramTextAsync(ruling.Id, ct);
        ruling.SearchText = PersianText.Normalize(
            $"{ruling.Question} {ruling.Question} {ruling.Answer} {ruling.FatwaNote} {diagramText}");

        // متن پرسش خودش بهترین عنوان متا است، چون کاربر دقیقاً
        // همان را در گوگل جستجو می‌کند
        ruling.MetaTitle = string.IsNullOrWhiteSpace(Input.MetaTitle)
            ? Truncate(ruling.Question, 70)
            : Input.MetaTitle.Trim();

        // در حکم نموداری پاسخ خالی است، پس متن پرسش تنها چیزی است که
        // می‌شود در نتیجه گوگل نشان داد. بدون این، توضیح متا خالی می‌ماند.
        ruling.MetaDescription = string.IsNullOrWhiteSpace(Input.MetaDescription)
            ? Truncate(string.IsNullOrWhiteSpace(ruling.Answer)
                ? ruling.Question
                : ReadingTime.Excerpt(ruling.Answer, 170), 170)
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

        // اتصال پرسش کاربر به حکم منتشرشده
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
            logger.LogInformation("ریدایرکت حکم از {Old} به {New}", previousSlug, ruling.Slug);
        }

        FlashKind = "ok";

        // حکم نموداریِ بدون گره، قدم بعدی‌اش ساختن نمودار است.
        // کاربر را همان‌جا می‌بریم تا دوباره دنبال دکمه‌اش نگردد.
        if (ruling.HasDiagram && !HasDiagramNodes)
        {
            Flash = isNew
                ? "حکم ثبت شد. حالا نمودار شرطی آن را بنویسید."
                : "تغییرات ذخیره شد. حالا نمودار شرطی را بنویسید.";
            return RedirectToPage("./Nemodar", new { id = ruling.Id });
        }

        Flash = isNew ? "حکم ثبت شد." : "تغییرات ذخیره شد.";
        return RedirectToPage("./Edit", new { id = ruling.Id });
    }

    /// <summary>متن همه شرط‌ها و حکم‌های نمودار، برای جستجو.</summary>
    private async Task<string> BuildDiagramTextAsync(int rulingId, CancellationToken ct)
    {
        var nodes = await db.RulingNodes.AsNoTracking()
            .Where(n => n.RulingId == rulingId)
            .Select(n => n.Text)
            .ToListAsync(ct);

        var verdicts = await db.RulingVerdicts.AsNoTracking()
            .Where(v => v.RulingNode.RulingId == rulingId)
            .Select(v => v.Text)
            .ToListAsync(ct);

        return string.Join(' ', nodes.Concat(verdicts));
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