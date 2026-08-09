using Microsoft.AspNetCore.Identity;
using NurSite.Infrastructure.Identity;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Extensions;

public static class IdentitySetup
{
    /// <summary>
    /// احراز هویت و مدیریت کاربران. ورود با شماره موبایل انجام می‌شود.
    /// </summary>
    public static IServiceCollection AddSiteIdentity(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // رمز عبور
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;

            // قفل حساب پس از تلاش‌های ناموفق
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;

            // نام کاربری همان شماره موبایل است
            options.User.RequireUniqueEmail = false;
            options.User.AllowedUserNameCharacters = "0123456789+";
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // بررسی مهر امنیتی هر ۲۴ ساعت به‌جای هر ۳۰ دقیقه.
        // با نشست دو هفته‌ای، بررسی مکرر یعنی کوئری بی‌مورد به دیتابیس.
        services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.FromHours(24);
        });

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/vorood";
            options.LogoutPath = "/khorooj";
            options.AccessDeniedPath = "/dastresi-nadarid";
            // کاربر دو هفته وارد می‌ماند. با SlidingExpiration هر بازدید
            // این مهلت را تازه می‌کند، پس کاربر فعال عملاً خارج نمی‌شود.
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.Name = "nur.auth";
        });

        return services;
    }
}