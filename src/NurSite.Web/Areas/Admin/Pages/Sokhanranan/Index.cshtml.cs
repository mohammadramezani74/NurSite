using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Application.Interfaces;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Identity;
using NurSite.Infrastructure.Persistence;
using NurSite.Web.Services;

namespace NurSite.Web.Areas.Admin.Pages.Sokhanranan;

/// <summary>
/// سخنرانان. فهرست و فرم در یک صفحه‌اند، مثل ابواب و منابع.
/// </summary>
[Authorize(Policy = Permissions.Media.Manage)]
public class IndexModel(AppDbContext db, ISlugService slugs, FileUploadService uploads) : PageModel
{
    public sealed record Row(Speaker Speaker, int LectureCount, int PublishedCount);

    public IReadOnlyList<Row> Rows { get; private set; } = [];

    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty] public IFormFile? PortraitFile { get; set; }

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام سخنران را بنویسید")]
        [StringLength(150)]
        [Display(Name = "نام کامل")]
        public string FullName { get; set; } = default!;

        [StringLength(150)]
        [Display(Name = "نشانی")]
        public string? Slug { get; set; }

        [StringLength(150)]
        [Display(Name = "عنوان")]
        public string? Title { get; set; }

        [Display(Name = "معرفی")]
        public string? Bio { get; set; }

        [Display(Name = "تصویر")]
        public string? PortraitPath { get; set; }

        [Display(Name = "فعال")]
        public bool IsActive { get; set; } = true;
    }

    public async Task OnGetAsync(int? edit, CancellationToken ct)
    {
        await LoadAsync(ct);

        if (edit is null) return;

        var speaker = Rows.FirstOrDefault(r => r.Speaker.Id == edit)?.Speaker;
        if (speaker is null) return;

        Input = new InputModel
        {
            Id = speaker.Id,
            FullName = speaker.FullName,
            Slug = speaker.Slug,
            Title = speaker.Title,
            Bio = speaker.Bio,
            PortraitPath = speaker.PortraitPath,
            IsActive = speaker.IsActive
        };
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (PortraitFile is not null && PortraitFile.Length > 0)
        {
            var upload = await uploads.SaveImageAsync(PortraitFile, "speakers", ct);
            if (!upload.Ok)
                ModelState.AddModelError("PortraitFile", upload.Error ?? "آپلود تصویر ناموفق بود.");
            else
                Input.PortraitPath = upload.Path;
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(ct);
            return Page();
        }

        var isNew = Input.Id == 0;
        var speaker = isNew
            ? new Speaker()
            : await db.Speakers.FirstOrDefaultAsync(s => s.Id == Input.Id, ct);

        if (speaker is null) return NotFound();

        var desired = string.IsNullOrWhiteSpace(Input.Slug) ? Input.FullName : Input.Slug;
        speaker.Slug = await slugs.GenerateUniqueAsync<Speaker>(
            desired, isNew ? null : speaker.Id, ct);

        speaker.FullName = Input.FullName.Trim();
        speaker.Title = Input.Title?.Trim();
        speaker.Bio = Input.Bio?.Trim();
        speaker.PortraitPath = Input.PortraitPath;
        speaker.IsActive = Input.IsActive;

        if (isNew) db.Speakers.Add(speaker);
        await db.SaveChangesAsync(ct);

        Flash = isNew ? "سخنران ثبت شد." : "تغییرات ذخیره شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var speaker = await db.Speakers.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (speaker is null) return NotFound();

        // کلید خارجی SetNull است، پس حذف سخنران سخنرانی‌ها را از بین نمی‌برد
        // ولی نامشان را می‌اندازد. بهتر است ادمین به‌جای حذف، غیرفعالش کند.
        var count = await db.Lectures.CountAsync(l => l.SpeakerId == id, ct);
        if (count > 0)
        {
            Flash = $"«{speaker.FullName}» به {count} سخنرانی متصل است و حذف نمی‌شود. " +
                    "برای پنهان کردنش، به‌جای حذف آن را غیرفعال کنید.";
            FlashKind = "warn";
            return RedirectToPage();
        }

        var portrait = speaker.PortraitPath;

        db.Speakers.Remove(speaker);
        await db.SaveChangesAsync(ct);

        // سخنران حذف فیزیکی می‌شود، پس تصویرش هم باید برود
        uploads.Delete(portrait);

        Flash = $"«{speaker.FullName}» حذف شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var speakers = await db.Speakers.AsNoTracking()
            .OrderBy(s => s.FullName)
            .ToListAsync(ct);

        var counts = await db.Lectures.AsNoTracking()
            .Where(l => l.SpeakerId != null)
            .GroupBy(l => l.SpeakerId!.Value)
            .Select(g => new
            {
                Id = g.Key,
                Total = g.Count(),
                Published = g.Count(l => l.Status == PublishStatus.Published)
            })
            .ToListAsync(ct);

        Rows = speakers.Select(s =>
        {
            var c = counts.FirstOrDefault(x => x.Id == s.Id);
            return new Row(s, c?.Total ?? 0, c?.Published ?? 0);
        }).ToList();
    }
}