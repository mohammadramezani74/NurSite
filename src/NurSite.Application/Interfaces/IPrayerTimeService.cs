using NurSite.Application.DTOs;

namespace NurSite.Application.Interfaces;

public interface IPrayerTimeService
{
    /// <summary>اوقات شرعی یک شهر در یک روز. نتیجه تا پایان همان روز کش می‌شود.</summary>
    Task<PrayerTimesDto> GetForDayAsync(int cityId, DateOnly localDate, CancellationToken ct = default);
}
