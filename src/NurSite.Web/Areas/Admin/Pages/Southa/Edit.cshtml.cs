using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Application.Services;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;
using NurSite.Web.Services;

namespace NurSite.Web.Areas.Admin.Pages.Southa;

public class EditModel(
    AppDbContext db,
    ISlugService slugs,
    FileUploadService uploads,
    ILogger<EditModel> logger) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty] public IFormFile? AudioFile { get; set; }
    [BindProperty] public IFormFile? CoverFile { get; set; }

    public SelectList SpeakerOptions { get; private set; } = default!;
    public SelectList SeriesOptions { get; private set; } = default!;
    public bool IsNew => Input.Id == 0;
    public string CanonicalBase { get; private set; } = "";

    /// <summary>حجم فایل فعلی، فقط برای نمایش.</summary>
    public long CurrentSizeBytes { get; private set; }

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    /// <summary>صوت از کجا می‌آید. با رادیو انتخاب می‌شود تا تکلیف روشن باشد.</summary>
    public enum AudioSource { Upload = 0, External = 1 }

    public class InputModel
    {
        public int Id { get; set; }

        [Display(Name = "نوع")]
        public AudioKind Kind { get; set; } = AudioKind.Lecture;

        [Required(ErrorMessage = "عنوان را بنویسید")]
        [StringLength(250, ErrorMessage = "عنوان نباید بیش از ۲۵۰ کاراکتر باشد")]
        [Display(Name = "عنوان")]
        public string Title { get; set; } = default!;

        [StringLength(250)]
        [Display(Name = "نشانی صفحه")]
        public string? Slug { get; set; }

        [StringLength(4000, ErrorMessage = "توضیح نباید بیش از ۴۰۰۰ کاراکتر باشد")]
        [Display(Name = "توضیح")]
        public string? Description { get; set; }

        [Display(Name = "منبع صوت")]
        public AudioSource Source { get; set; } = AudioSource.Upload;

        [Display(Name = "فایل روی سرور")]
        public string? AudioPath { get; set; }

        [StringLength(600)]
        [Url(ErrorMessage = "نشانی معتبر نیست")]
        [Display(Name = "نشانی فایل بیرونی")]
        public string? ExternalAudioUrl { get; set; }

        [Display(Name = "مدت")]
        public string? Duration { get; set; }

        [Display(Name = "گوینده")]
        public int? SpeakerId { get; set; }

        [Display(Name = "مجموعه")]
        public int? LectureSeriesId { get; set; }

        [Range(1, 999, ErrorMessage = "شماره جلسه باید بین ۱ تا ۹۹۹ باشد")]
        [Display(Name = "شماره جلسه")]
        public int? EpisodeNumber { get; set; }

        [Display(Name = "تاریخ ضبط")]
        public string? RecordedOn { get; set; }

        [Display(Name = "دسترسی دانلود")]
        public DownloadAccess DownloadAccess { get; set; } = DownloadAccess.Everyone;

        [Display(Name = "وضعیت")]
        public PublishStatus Status { get; set; } = PublishStatus.Draft;

        // ---------- سئو ----------
        [Display(Name = "تصویر")]
        public string? OgImagePath { get; set; }

        [StringLength(70, ErrorMessage = "عنوان متا نباید بیش از ۷۰ کاراکتر باشد")]
        [Display(Name = "عنوان متا")]
        public string? MetaTitle { get; set; }

        [StringLength(170, ErrorMessage = "توضیح متا نباید بیش از ۱۷۰ کاراکتر باشد")]
        [Display(Name = "توضیح متا")]
        public string? MetaDescription { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int? id, AudioKind? kind, CancellationToken ct)
    {
        await LoadOptionsAsync(ct);

        if (id is null or 0)
        {
            // وقتی از فهستِ فیلترشده «صوت تازه» زده می‌شود، نوع از پیش انتخاب باشد
            if (kind is not null) Input.Kind = kind.Value;
            return Page();
        }

        var item = await db.Lectures.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);
        if (item is null) return NotFound();

        CurrentSizeBytes = item.FileSizeBytes;

        Input = new InputModel
        {
            Id = item.Id,
            Kind = item.Kind,
            Title = item.Title,
            Slug = item.Slug,
            Description = item.Description,
            Source = item.IsExternal ? AudioSource.External : AudioSource.Upload,
            AudioPath = item.AudioPath,
            ExternalAudioUrl = item.ExternalAudioUrl,
            Duration = item.DurationSeconds > 0 ? AudioKinds.FormatDuration(item.DurationSeconds) : null,
            SpeakerId = item.SpeakerId,
            LectureSeriesId = item.LectureSeriesId,
            EpisodeNumber = item.EpisodeNumber,
            RecordedOn = PersianDateText.Format(item.RecordedOnUtc),
            DownloadAccess = item.DownloadAccess,
            Status = item.Status,
            OgImagePath = item.OgImagePath,
            MetaTitle = item.MetaTitle,
            MetaDescription = item.MetaDescription
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadOptionsAsync(ct);

        int? uploadedDuration = null;
        long? uploadedSize = null;
        string? replacedFile = null;

        if (Input.Source == AudioSource.Upload)
        {
            if (AudioFile is not null && AudioFile.Length > 0)
            {
                var upload = await uploads.SaveAudioAsync(AudioFile, "lectures", ct);
                if (!upload.Ok)
                {
                    ModelState.AddModelError("AudioFile", upload.Error ?? "آپلود فایل صوتی ناموفق بود.");
                }
                else
                {
                    // فایل قبلی بعد از ذخیره موفق حذف می‌شود، نه پیش از آن
                    replacedFile = Input.AudioPath;
                    Input.AudioPath = upload.Path;
                    uploadedSize = upload.SizeBytes;
                    uploadedDuration = upload.DurationSeconds;
                }
            }

            if (string.IsNullOrWhiteSpace(Input.AudioPath))
                ModelState.AddModelError("AudioFile", "فایل صوتی را انتخاب کنید.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(Input.ExternalAudioUrl))
                ModelState.AddModelError("Input.ExternalAudioUrl", "نشانی فایل صوتی را بنویسید.");
        }

        // مدت را کاربر می‌تواند دستی بنویسد؛ اگر خالی باشد و فایل تازه
        // آپلود شده باشد، از خود فایل خوانده می‌شود
        var seconds = 0;
        var hasManualDuration = AudioKinds.TryParseDuration(Input.Duration, out seconds);
        if (!hasManualDuration && !string.IsNullOrWhiteSpace(Input.Duration))
            ModelState.AddModelError("Input.Duration", "مدت را به شکل ۴:۱۲ یا ۱:۰۵:۳۰ بنویسید.");

        DateTime? recordedOn = null;
        if (!string.IsNullOrWhiteSpace(Input.RecordedOn))
        {
            if (PersianDateText.TryParse(Input.RecordedOn, out var parsed))
                recordedOn = parsed;
            else
                ModelState.AddModelError("Input.RecordedOn", "تاریخ را به شکل ۱۴۰۵/۰۵/۳۰ بنویسید.");
        }

        if (CoverFile is not null && CoverFile.Length > 0)
        {
            var cover = await uploads.SaveImageAsync(CoverFile, "lectures", ct);
            if (!cover.Ok)
                ModelState.AddModelError("CoverFile", cover.Error ?? "آپلود تصویر ناموفق بود.");
            else
                Input.OgImagePath = cover.Path;
        }

        if (!ModelState.IsValid)
        {
            CurrentSizeBytes = uploadedSize ?? 0;
            return Page();
        }

        var isNew = Input.Id == 0;
        var item = isNew
            ? new Lecture()
            : await db.Lectures.FirstOrDefaultAsync(l => l.Id == Input.Id, ct);

        if (item is null) return NotFound();

        var previousKind = item.Kind;
        var previousSlug = item.Slug;

        var desiredSlug = string.IsNullOrWhiteSpace(Input.Slug) ? Input.Title : Input.Slug;
        item.Slug = await slugs.GenerateUniqueAsync<Lecture>(
            desiredSlug, isNew ? null : item.Id, ct);

        item.Kind = Input.Kind;
        item.Title = Input.Title.Trim();
        item.Description = Input.Description?.Trim();
        item.SpeakerId = Input.SpeakerId;
        item.LectureSeriesId = Input.LectureSeriesId;
        item.EpisodeNumber = Input.LectureSeriesId is null ? null : Input.EpisodeNumber;
        item.RecordedOnUtc = recordedOn;
        item.DownloadAccess = Input.DownloadAccess;

        // ---------- منبع صوت ----------
        if (Input.Source == AudioSource.Upload)
        {
            item.AudioPath = Input.AudioPath;
            item.ExternalAudioUrl = null;
            if (uploadedSize is not null) item.FileSizeBytes = uploadedSize.Value;
        }
        else
        {
            item.ExternalAudioUrl = Input.ExternalAudioUrl!.Trim();

            // فایل قبلی روی سرور دیگر استفاده نمی‌شود و بی‌جهت جا می‌گیرد
            if (!string.IsNullOrWhiteSpace(item.AudioPath))
            {
                uploads.Delete(item.AudioPath);
                item.AudioPath = null;
            }
            item.FileSizeBytes = 0;
        }

        item.DurationSeconds = hasManualDuration
            ? seconds
            : uploadedDuration ?? item.DurationSeconds;

        // ---------- سئو ----------
        item.OgImagePath = Input.OgImagePath;

        item.MetaTitle = string.IsNullOrWhiteSpace(Input.MetaTitle)
            ? Truncate(item.Title, 70)
            : Input.MetaTitle.Trim();

        // توضیح ممکن است HTML باشد؛ برای متا باید متن ساده برود
        item.MetaDescription = string.IsNullOrWhiteSpace(Input.MetaDescription)
            ? Truncate(PlainText(item.Description) is { Length: > 0 } text
                ? text
                : $"{AudioKinds.Label(item.Kind)} {item.Title}", 170)
            : Input.MetaDescription.Trim();

        // نام گوینده و مجموعه هم ایندکس می‌شوند، چون کاربر معمولاً
        // «فلان مداح فلان شب» را جستجو می‌کند نه عنوان دقیق را
        var speakerName = Input.SpeakerId is null
            ? null
            : (await db.Speakers.AsNoTracking()
                .Where(s => s.Id == Input.SpeakerId)
                .Select(s => s.FullName).FirstOrDefaultAsync(ct));

        var seriesTitle = Input.LectureSeriesId is null
            ? null
            : (await db.LectureSeries.AsNoTracking()
                .Where(s => s.Id == Input.LectureSeriesId)
                .Select(s => s.Title).FirstOrDefaultAsync(ct));

        item.SearchText = PersianText.Normalize(
            $"{item.Title} {item.Title} {speakerName} {seriesTitle} {AudioKinds.Label(item.Kind)} {item.Description}");

        item.Status = Input.Status;
        if (Input.Status == PublishStatus.Published)
            item.PublishedAtUtc ??= DateTime.UtcNow;

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (isNew)
        {
            item.CreatedById = userId;
            db.Lectures.Add(item);
        }
        else
        {
            item.UpdatedById = userId;
        }

        await db.SaveChangesAsync(ct);

        if (replacedFile is not null) uploads.Delete(replacedFile);

        // ---------- ریدایرکت ۳۰۱ ----------
        // نشانی هم با تغییر اسلاگ عوض می‌شود هم با تغییر نوع، چون بخش
        // اول نشانی از نوع می‌آید. هر دو باید هدایت شوند وگرنه لینک‌ها می‌شکنند
        if (!isNew && !string.IsNullOrEmpty(previousSlug) &&
            (previousSlug != item.Slug || previousKind != item.Kind))
        {
            var from = AudioKinds.Url(previousKind, previousSlug);
            var to = AudioKinds.Url(item.Kind, item.Slug);
            await AddRedirectAsync(from, to, ct);
            logger.LogInformation("ریدایرکت صوت از {Old} به {New}", from, to);
        }

        Flash = isNew ? "صوت ثبت شد." : "تغییرات ذخیره شد.";
        FlashKind = "ok";
        return RedirectToPage("./Edit", new { id = item.Id });
    }

    private async Task AddRedirectAsync(string from, string to, CancellationToken ct)
    {
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

        // زنجیره ریدایرکت ساخته نشود
        var chained = await db.UrlRedirects.Where(r => r.ToPath == from).ToListAsync(ct);
        foreach (var r in chained) r.ToPath = to;

        await db.SaveChangesAsync(ct);
    }

    private async Task LoadOptionsAsync(CancellationToken ct)
    {
        var speakers = await db.Speakers.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.FullName).ToListAsync(ct);
        SpeakerOptions = new SelectList(speakers, nameof(Speaker.Id), nameof(Speaker.FullName));

        var series = await db.LectureSeries.AsNoTracking()
            .OrderBy(s => s.Title).ToListAsync(ct);
        SeriesOptions = new SelectList(series, nameof(LectureSeries.Id), nameof(LectureSeries.Title));

        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        CanonicalBase = (settings?.CanonicalBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
    }

    private static string PlainText(string? html) =>
        string.IsNullOrWhiteSpace(html) ? string.Empty : PersianText.Normalize(html);

    /// <summary>بریدن متن با رعایت دقیق سقف؛ سه‌نقطه هم یک کاراکتر است.</summary>
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