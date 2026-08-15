using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NurSite.Application.Interfaces;
using NurSite.Infrastructure.Persistence;
using NurSite.Infrastructure.Services;

namespace NurSite.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// دیتابیس و سرویس‌های زیرساخت.
    /// پیکربندی Identity و کوکی اینجا نیست — آن‌ها مفاهیم وب هستند
    /// و در لایه Web ثبت می‌شوند.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                config.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddMemoryCache();

        services.AddScoped<ISlugService, SlugService>();
        services.AddSingleton<IPersianDateService, PersianDateService>();
        services.AddScoped<IPrayerTimeService, PrayerTimeService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IOccasionService, OccasionService>();
        services.AddScoped<IRulingDiagramService, RulingDiagramService>();

        // اطلاع‌رسانی: فعلاً فقط لاگ می‌گیرد و کاربر با کد رهگیری پیگیری می‌کند.
        // برای فعال کردن پیامک، فقط همین خط به پیاده‌سازی تازه تغییر می‌کند.
        services.AddScoped<INotificationService, LoggingNotificationService>();

        // ثبت سرویس پیامک در لایه Web است، نه اینجا: به HttpClient و
        // بایندر پیکربندی نیاز دارد که هر دو بسته‌های وب‌اند.
        services.AddScoped<ILoginCodeService, LoginCodeService>();

        return services;
    }
}