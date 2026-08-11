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

namespace NurSite.Web.Areas.Admin.Pages.Maghalat;

public class EditModel(
    AppDbContext db,
    ISlugService slugs,
    FileUploadService uploads,
    ILogger<EditModel> logger) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty] public IFormFile? CoverFile { get; set; }

    public SelectList CategoryOptions { get; private set; } = default!;
    public bool IsNew => Input.Id == 0;
    public string CanonicalBase { get; private set; } = "";

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "عنوان مقاله را بنویسید")]
        [StringLength(250, ErrorMessage = "عنوان نباید بیش از ۲۵۰ کاراکتر باشد")]
        [Display(Name = "عنوان")]
        public string Title { get; set; } = default!;

        [StringLength(250)]
        [Display(Name = "نشانی صفحه")]
        public string? Slug { get; set; }

        // nullable است تا وقتی گزینه خالی ارسال شود، بایندر شکست نخورد
        // و پیام فارسی خودمان نمایش داده شود نه پیام پیش‌فرض انگلیسی
        [Required(ErrorMessage = "دسته‌بندی را انتخاب کنید")]
        [Display(Name = "دسته‌بندی")]
        public int? CategoryId { get; set; }

        [StringLength(500, ErrorMessage = "خلاصه نباید بیش از ۵۰۰ کاراکتر باشد")]
        [Display(Name = "خلاصه")]
        public string? Summary { get; set; }

        [Required(ErrorMessage = "متن مقاله را بنویسید")]
        [Display(Name = "متن مقاله")]
        public string Body { get; set; } = default!;

        [StringLength(150)]
        [Display(Name = "نام نویسنده")]
        public string? AuthorDisplayName { get; set; }

        [Display(Name = "تصویر شاخص")]
        public string? CoverImagePath { get; set; }

        [StringLength(250)]
        [Display(Name = "متن جایگزین تصویر")]
        public string? CoverImageAlt { get; set; }

        [Display(Name = "مقاله ویژه")]
        public bool IsFeatured { get; set; }

        [Display(Name = "وضعیت")]
        public PublishStatus Status { get; set; } = PublishStatus.Draft;

        // ---------- سئو ----------
        [StringLength(70, ErrorMessage = "عنوان متا نباید بیش از ۷۰ کاراکتر باشد")]
        [Display(Name = "عنوان متا")]
        public string? MetaTitle { get; set; }

        [StringLength(170, ErrorMessage = "توضیح متا نباید بیش از ۱۷۰ کاراکتر باشد")]
        [Display(Name = "توضیح متا")]
        public string? MetaDescription { get; set; }

        [StringLength(400)]
        [Display(Name = "تصویر اشتراک‌گذاری")]
        public string? OgImagePath { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int? id, CancellationToken ct)
    {
        await LoadOptionsAsync(ct);

        if (id is null or 0) return Page();

        var article = await db.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (article is null) return NotFound();

        Input = new InputModel
        {
            Id = article.Id,
            Title = article.Title,
            Slug = article.Slug,
            CategoryId = article.CategoryId,
            Summary = article.Summary,
            Body = article.Body,
            AuthorDisplayName = article.AuthorDisplayName,
            CoverImagePath = article.CoverImagePath,
            CoverImageAlt = article.CoverImageAlt,
            IsFeatured = article.IsFeatured,
            Status = article.Status,
            MetaTitle = article.MetaTitle,
            MetaDescription = article.MetaDescription,
            OgImagePath = article.OgImagePath
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadOptionsAsync(ct);

        // اگر تصویر شاخص هست، متن جایگزین اجباری است.
        // این هم برای دسترس‌پذیری لازم است هم برای سئوی تصاویر.
        if (!string.IsNullOrWhiteSpace(Input.CoverImagePath) && string.IsNullOrWhiteSpace(Input.CoverImageAlt))
            ModelState.AddModelError("Input.CoverImageAlt", "برای تصویر شاخص، متن جایگزین لازم است.");

        if (CoverFile is not null && CoverFile.Length > 0)
        {
            var upload = await uploads.SaveImageAsync(CoverFile, "articles", ct);
            if (!upload.Ok)
                ModelState.AddModelError("CoverFile", upload.Error ?? "آپلود تصویر ناموفق بود.");
            else
                Input.CoverImagePath = upload.Path;
        }

        if (!ModelState.IsValid) return Page();

        var isNew = Input.Id == 0;
        var article = isNew
            ? new Article()
            : await db.Articles.FirstOrDefaultAsync(a => a.Id == Input.Id, ct);

        if (article is null) return NotFound();

        var previousSlug = article.Slug;

        // ---------- اسلاگ ----------
        var desiredSlug = string.IsNullOrWhiteSpace(Input.Slug) ? Input.Title : Input.Slug;
        var newSlug = await slugs.GenerateUniqueAsync<Article>(
            desiredSlug, isNew ? null : article.Id, ct);

        article.Title = Input.Title.Trim();
        article.Slug = newSlug;
        article.CategoryId = Input.CategoryId!.Value;
        article.Summary = Input.Summary?.Trim();
        article.Body = Input.Body;
        article.AuthorDisplayName = string.IsNullOrWhiteSpace(Input.AuthorDisplayName)
            ? User.Identity?.Name
            : Input.AuthorDisplayName.Trim();
        article.CoverImagePath = Input.CoverImagePath;
        article.CoverImageAlt = Input.CoverImageAlt?.Trim();
        article.IsFeatured = Input.IsFeatured;

        // ---------- سئو ----------
        // اگر پر نشده باشند، از عنوان و خلاصه ساخته می‌شوند تا هیچ صفحه‌ای بدون متا نماند
        article.MetaTitle = string.IsNullOrWhiteSpace(Input.MetaTitle)
            ? Truncate(article.Title, 70)
            : Input.MetaTitle.Trim();

        article.MetaDescription = string.IsNullOrWhiteSpace(Input.MetaDescription)
            ? Truncate(string.IsNullOrWhiteSpace(article.Summary)
                ? ReadingTime.Excerpt(article.Body, 170)
                : article.Summary, 170)
            : Input.MetaDescription.Trim();

        article.OgImagePath = string.IsNullOrWhiteSpace(Input.OgImagePath)
            ? article.CoverImagePath
            : Input.OgImagePath;

        article.ReadingMinutes = ReadingTime.Estimate(article.Body);

        // متن جستجو از عنوان و خلاصه و بدنه ساخته می‌شود.
        // عنوان دو بار می‌آید تا در امتیازدهی وزن بیشتری بگیرد.
        article.SearchText = PersianText.Normalize(
            $"{article.Title} {article.Title} {article.Summary} {article.Body}");

        // ---------- وضعیت انتشار ----------
        article.Status = Input.Status;
        if (Input.Status == PublishStatus.Published)
            article.PublishedAtUtc ??= DateTime.UtcNow;

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (isNew)
        {
            article.AuthorId = userId;
            article.CreatedById = userId;
            db.Articles.Add(article);
        }
        else
        {
            article.UpdatedById = userId;
        }

        await db.SaveChangesAsync(ct);

        // ---------- ریدایرکت ۳۰۱ ----------
        // اگر نشانی عوض شده، آدرس قدیمی باید به جدید هدایت شود
        // وگرنه رتبه‌ای که در گوگل گرفته از بین می‌رود و لینک‌های بیرونی می‌شکنند
        if (!isNew && !string.IsNullOrEmpty(previousSlug) && previousSlug != article.Slug)
        {
            await AddRedirectAsync(previousSlug, article.Slug, ct);
            logger.LogInformation("ریدایرکت از {Old} به {New} ثبت شد", previousSlug, article.Slug);
        }

        Flash = isNew ? "مقاله ساخته شد." : "تغییرات ذخیره شد.";
        FlashKind = "ok";
        return RedirectToPage("./Edit", new { id = article.Id });
    }

    private async Task AddRedirectAsync(string oldSlug, string newSlug, CancellationToken ct)
    {
        var from = $"/maghalat/{oldSlug}";
        var to = $"/maghalat/{newSlug}";

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

        // اگر قبلاً چیزی به نشانی قدیمی هدایت می‌شد، حالا باید به نشانی جدید برود
        // تا زنجیره ریدایرکت ساخته نشود
        var chained = await db.UrlRedirects.Where(r => r.ToPath == from).ToListAsync(ct);
        foreach (var r in chained) r.ToPath = to;

        await db.SaveChangesAsync(ct);
    }

    private async Task LoadOptionsAsync(CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
            .ToListAsync(ct);

        CategoryOptions = new SelectList(categories, nameof(Category.Id), nameof(Category.Title));

        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        CanonicalBase = (settings?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
    }

    /// <summary>
    /// بریدن متن با رعایت دقیق سقف. سه‌نقطه هم یک کاراکتر است و باید
    /// در همان سقف بگنجد، وگرنه دیتابیس خطای truncation می‌دهد.
    /// </summary>
    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        value = value.Trim();
        if (value.Length <= max) return value;

        var cut = value[..(max - 1)].TrimEnd();

        // ترجیحاً در مرز کلمه ببر، نه وسط کلمه
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > max / 2) cut = cut[..lastSpace];

        return cut + "…";
    }
}