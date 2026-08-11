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

        return services;
    }
}