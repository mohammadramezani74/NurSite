using NurSite.Application.DTOs;

namespace NurSite.Application.Interfaces;

public interface IPrayerTimeService
{
    /// <summary>اوقات شرعی یک شهر در یک روز. نتیجه تا پایان همان روز کش می‌شود.</summary>
    Task<PrayerTimesDto> GetForDayAsync(int cityId, DateOnly localDate, CancellationToken ct = default);

    /// <summary>
    /// اوقات شرعی یک بازه پیوسته از روزها — برای نمایش تقویم ماهانه.
    /// چون محاسبه سبک است، یکجا انجام می‌شود نه روز به روز.
    /// </summary>
    Task<IReadOnlyList<PrayerTimesDto>> GetForRangeAsync(
        int cityId, DateOnly from, DateOnly to, CancellationToken ct = default);
}