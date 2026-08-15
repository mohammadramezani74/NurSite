using NurSite.Application.Interfaces;
using NurSite.Infrastructure.Services;

namespace NurSite.Web.Extensions;

public static class SmsSetup
{
    /// <summary>
    /// سرویس پیامک و تنظیمات کد یک‌بارمصرف.
    ///
    /// چرا اینجا و نه در AddInfrastructure؟ چون AddHttpClient و بایندر
    /// پیکربندی، بسته‌های وب‌اند و پروژه Infrastructure یک کتابخانه ساده
    /// است. همان قاعده‌ای که Identity هم به خاطرش اینجا ثبت می‌شود.
    /// </summary>
    public static IServiceCollection AddSmsServices(
        this IServiceCollection services, IConfiguration config)
    {
        services.Configure<SmsOptions>(config.GetSection("Sms"));
        services.Configure<LoginCodeOptions>(config.GetSection("LoginCode"));

        var sms = config.GetSection("Sms").Get<SmsOptions>() ?? new SmsOptions();

        if (sms.UseFakeSender || string.IsNullOrWhiteSpace(sms.ApiKey))
        {
            // بدون کلید، سرویس واقعی جز خطا چیزی نمی‌دهد. در توسعه کد در
            // لاگ نوشته می‌شود تا جریان ورود بدون خرج اعتبار تست شود.
            services.AddScoped<ISmsSender, FakeSmsSender>();
            return services;
        }

        services.AddHttpClient<ISmsSender, SmsIrSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.sms.ir/");
            client.DefaultRequestHeaders.Add("x-api-key", sms.ApiKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            // بدون مهلت، یک سرویس کند صفحه ورود را قفل می‌کند
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}