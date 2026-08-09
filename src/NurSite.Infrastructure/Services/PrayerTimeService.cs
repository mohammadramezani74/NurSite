using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NurSite.Application.DTOs;
using NurSite.Application.Interfaces;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Infrastructure.Services;

/// <summary>
/// محاسبه اوقات شرعی به روش مؤسسه ژئوفیزیک دانشگاه تهران:
/// زاویه ۱۷.۷ درجه برای اذان صبح و ۴.۵ درجه برای اذان مغرب.
/// نتیجه تا پایان همان روز کش می‌شود تا برای هر بازدید دوباره محاسبه نشود.
/// </summary>
public sealed class PrayerTimeService(AppDbContext db, IMemoryCache cache) : IPrayerTimeService
{
    private const double FajrAngle = 17.7;
    private const double MaghribAngle = 4.5;

    public async Task<PrayerTimesDto> GetForDayAsync(int cityId, DateOnly localDate, CancellationToken ct = default)
    {
        var key = $"prayer:{cityId}:{localDate:yyyy-MM-dd}";
        if (cache.TryGetValue(key, out PrayerTimesDto? cached) && cached is not null)
            return cached;

        var city = await db.Cities.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cityId, ct)
                   ?? throw new InvalidOperationException($"شهر با شناسه {cityId} پیدا نشد.");

        var result = Calculate(city.Id, city.Name, city.Latitude, city.Longitude, city.Elevation, localDate);

        // تا پایان روز محلی معتبر است
        var expiresIn = localDate.ToDateTime(TimeOnly.MaxValue) - localDate.ToDateTime(TimeOnly.FromDateTime(DateTime.Now));
        cache.Set(key, result, expiresIn > TimeSpan.Zero ? expiresIn : TimeSpan.FromHours(1));
        return result;
    }

    private static PrayerTimesDto Calculate(
        int cityId, string cityName, double lat, double lng, double elevation, DateOnly date)
    {
        // آفست ایران، ۳ ساعت و ۳۰ دقیقه
        const double tzOffset = 3.5;

        var dayOfYear = date.DayOfYear;

        // میل خورشید و معادله زمان — تقریب استاندارد نجومی
        var gamma = 2 * Math.PI / 365 * (dayOfYear - 1 + 0.5);
        var declination = 0.006918
            - 0.399912 * Math.Cos(gamma) + 0.070257 * Math.Sin(gamma)
            - 0.006758 * Math.Cos(2 * gamma) + 0.000907 * Math.Sin(2 * gamma)
            - 0.002697 * Math.Cos(3 * gamma) + 0.001480 * Math.Sin(3 * gamma);

        var eqTime = 229.18 * (0.000075
            + 0.001868 * Math.Cos(gamma) - 0.032077 * Math.Sin(gamma)
            - 0.014615 * Math.Cos(2 * gamma) - 0.040849 * Math.Sin(2 * gamma));

        // ظهر شرعی
        var dhuhr = 12.0 + tzOffset - lng / 15.0 - eqTime / 60.0;

        // اصلاح افق به‌خاطر ارتفاع از سطح دریا
        var horizonDip = 0.0347 * Math.Sqrt(Math.Max(elevation, 0));

        double HourAngle(double angleDeg)
        {
            var latRad = Deg2Rad(lat);
            var cosH = (Math.Cos(Deg2Rad(angleDeg)) - Math.Sin(declination) * Math.Sin(latRad))
                       / (Math.Cos(declination) * Math.Cos(latRad));
            cosH = Math.Clamp(cosH, -1.0, 1.0); // در عرض‌های بالا ممکن است خارج از بازه شود
            return Rad2Deg(Math.Acos(cosH)) / 15.0;
        }

        var sunriseOffset = HourAngle(90.833 + horizonDip);
        var fajrOffset    = HourAngle(90 + FajrAngle);
        var maghribOffset = HourAngle(90 + MaghribAngle);

        var fajr    = dhuhr - fajrOffset;
        var sunrise = dhuhr - sunriseOffset;
        var sunset  = dhuhr + sunriseOffset;
        var maghrib = dhuhr + maghribOffset;

        // نیمه‌شب شرعی: وسط فاصله مغرب تا اذان صبح روز بعد
        var midnight = maghrib + ((fajr + 24) - maghrib) / 2;
        if (midnight >= 24) midnight -= 24;

        return new PrayerTimesDto(
            cityId, cityName, date,
            ToTimeOnly(fajr), ToTimeOnly(sunrise), ToTimeOnly(dhuhr),
            ToTimeOnly(sunset), ToTimeOnly(maghrib), ToTimeOnly(midnight));
    }

    private static double Deg2Rad(double d) => d * Math.PI / 180.0;
    private static double Rad2Deg(double r) => r * 180.0 / Math.PI;

    private static TimeOnly ToTimeOnly(double hours)
    {
        hours = ((hours % 24) + 24) % 24;
        var totalMinutes = (int)Math.Round(hours * 60);
        if (totalMinutes >= 1440) totalMinutes -= 1440;
        return new TimeOnly(totalMinutes / 60, totalMinutes % 60);
    }
}
