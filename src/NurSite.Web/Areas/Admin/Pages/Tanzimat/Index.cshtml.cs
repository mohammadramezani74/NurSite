using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NurSite.Application.Interfaces;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Areas.Admin.Pages.Tanzimat;

public class IndexModel(
    AppDbContext db,
    IMemoryCache cache,
    IPersianDateService dates) : PageModel
{
    private static readonly UmAlQuraCalendar Hijri = new();

    [BindProperty] public InputModel Input { get; set; } = new();
    public SelectList CityOptions { get; private set; } = default!;

    /// <summary>تاریخ قمری امروز با آفست فعلی، تا ادمین بتواند با تقویم مقایسه کند.</summary>
    public string HijriToday { get; private set; } = "";
    public string HijriWithoutOffset { get; private set; } = "";

    [TempData] public string? Flash { get; set; }
    [TempData] public string? FlashKind { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "نام سایت را بنویسید")]
        [StringLength(150)]
        [Display(Name = "نام سایت")]
        public string SiteName { get; set; } = default!;

        [StringLength(300)]
        [Display(Name = "شعار")]
        public string? Tagline { get; set; }

        [Required(ErrorMessage = "نشانی مبنا را بنویسید")]
        [StringLength(250)]
        [Url(ErrorMessage = "نشانی معتبر نیست")]
        [Display(Name = "نشانی مبنای سایت")]
        public string CanonicalBaseUrl { get; set; } = default!;

        [StringLength(70)]
        [Display(Name = "عنوان متای پیش‌فرض")]
        public string? DefaultMetaTitle { get; set; }

        [StringLength(170)]
        [Display(Name = "توضیح متای پیش‌فرض")]
        public string? DefaultMetaDescription { get; set; }

        [Display(Name = "پوسته پیش‌فرض")]
        public SiteTheme DefaultTheme { get; set; }

        [Display(Name = "اجازه انتخاب پوسته به بازدیدکننده")]
        public bool AllowUserThemeChoice { get; set; }

        [Display(Name = "تغییر خودکار پوسته در مناسبت‌ها")]
        public bool EnableOccasionTheme { get; set; }

        [Display(Name = "شهر پیش‌فرض اوقات شرعی")]
        public int? DefaultCityId { get; set; }

        [Range(-3, 3, ErrorMessage = "اختلاف باید بین منفی سه تا مثبت سه باشد")]
        [Display(Name = "اختلاف تقویم قمری")]
        public int HijriDayOffset { get; set; }

        [StringLength(500)]
        [Display(Name = "نشانی")]
        public string? ContactAddress { get; set; }

        [StringLength(30)]
        [Display(Name = "تلفن")]
        public string? ContactPhone { get; set; }

        [StringLength(200)]
        [EmailAddress(ErrorMessage = "رایانامه معتبر نیست")]
        [Display(Name = "رایانامه")]
        public string? ContactEmail { get; set; }

        [StringLength(200)]
        [Display(Name = "ساعات کاری")]
        public string? WorkingHours { get; set; }

        [StringLength(300)]
        [Display(Name = "تلگرام")]
        public string? TelegramUrl { get; set; }

        [StringLength(300)]
        [Display(Name = "اینستاگرام")]
        public string? InstagramUrl { get; set; }

        [StringLength(300)]
        [Display(Name = "آپارات")]
        public string? AparatUrl { get; set; }

        [Display(Name = "حالت تعمیر")]
        public bool IsMaintenanceMode { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        if (settings is null) return NotFound();

        Input = new InputModel
        {
            SiteName = settings.SiteName,
            Tagline = settings.Tagline,
            CanonicalBaseUrl = settings.CanonicalBaseUrl,
            DefaultMetaTitle = settings.DefaultMetaTitle,
            DefaultMetaDescription = settings.DefaultMetaDescription,
            DefaultTheme = settings.DefaultTheme,
            AllowUserThemeChoice = settings.AllowUserThemeChoice,
            EnableOccasionTheme = settings.EnableOccasionTheme,
            DefaultCityId = settings.DefaultCityId,
            HijriDayOffset = settings.HijriDayOffset,
            ContactAddress = settings.ContactAddress,
            ContactPhone = settings.ContactPhone,
            ContactEmail = settings.ContactEmail,
            WorkingHours = settings.WorkingHours,
            TelegramUrl = settings.TelegramUrl,
            InstagramUrl = settings.InstagramUrl,
            AparatUrl = settings.AparatUrl,
            IsMaintenanceMode = settings.IsMaintenanceMode
        };

        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(ct);
            return Page();
        }

        var settings = await db.SiteSettings.FirstOrDefaultAsync(ct);
        if (settings is null) return NotFound();

        settings.SiteName = Input.SiteName.Trim();
        settings.Tagline = Input.Tagline?.Trim();
        settings.CanonicalBaseUrl = Input.CanonicalBaseUrl.Trim().TrimEnd('/');
        settings.DefaultMetaTitle = Input.DefaultMetaTitle?.Trim();
        settings.DefaultMetaDescription = Input.DefaultMetaDescription?.Trim();
        settings.DefaultTheme = Input.DefaultTheme;
        settings.AllowUserThemeChoice = Input.AllowUserThemeChoice;
        settings.EnableOccasionTheme = Input.EnableOccasionTheme;
        settings.DefaultCityId = Input.DefaultCityId;
        settings.HijriDayOffset = Input.HijriDayOffset;
        settings.ContactAddress = Input.ContactAddress?.Trim();
        settings.ContactPhone = Input.ContactPhone?.Trim();
        settings.ContactEmail = Input.ContactEmail?.Trim();
        settings.WorkingHours = Input.WorkingHours?.Trim();
        settings.TelegramUrl = Input.TelegramUrl?.Trim();
        settings.InstagramUrl = Input.InstagramUrl?.Trim();
        settings.AparatUrl = Input.AparatUrl?.Trim();
        settings.IsMaintenanceMode = Input.IsMaintenanceMode;

        await db.SaveChangesAsync(ct);

        // تنظیمات و مناسبت‌ها کش می‌شوند؛ بدون پاک کردن، تغییر دیر اثر می‌کند
        cache.Remove("site:settings");
        ClearOccasionCache();

        Flash = "تنظیمات ذخیره شد.";
        FlashKind = "ok";
        return RedirectToPage();
    }

    private void ClearOccasionCache()
    {
        // MemoryCache امکان حذف با الگو ندارد، پس کلیدهای امروز را دستی پاک می‌کنیم
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        cache.Remove($"theme:occasion:{today}");
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var cities = await db.Cities.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync(ct);
        CityOptions = new SelectList(cities, nameof(City.Id), nameof(City.Name));

        HijriToday = dates.ToHijriDate(DateTime.UtcNow, Input.HijriDayOffset);
        HijriWithoutOffset = dates.ToHijriDate(DateTime.UtcNow, 0);
    }
}