namespace NurSite.Application.DTOs;

/// <summary>اوقات شرعی یک روز. همه زمان‌ها به وقت محلی همان شهر هستند.</summary>
public sealed record PrayerTimesDto(
    int CityId,
    string CityName,
    DateOnly LocalDate,
    TimeOnly Fajr,      // اذان صبح
    TimeOnly Sunrise,   // طلوع آفتاب
    TimeOnly Dhuhr,     // اذان ظهر
    TimeOnly Sunset,    // غروب آفتاب
    TimeOnly Maghrib,   // اذان مغرب
    TimeOnly Midnight   // نیمه‌شب شرعی
)
{
    /// <summary>وقت بعدی نسبت به زمان داده‌شده، به‌همراه نامش.</summary>
    public (string Name, TimeOnly At)? NextAfter(TimeOnly now)
    {
        var all = new (string Name, TimeOnly At)[]
        {
            ("اذان صبح", Fajr), ("طلوع آفتاب", Sunrise), ("اذان ظهر", Dhuhr),
            ("غروب آفتاب", Sunset), ("اذان مغرب", Maghrib), ("نیمه‌شب شرعی", Midnight)
        };
        foreach (var item in all)
            if (item.At > now) return item;
        return null;
    }
}
