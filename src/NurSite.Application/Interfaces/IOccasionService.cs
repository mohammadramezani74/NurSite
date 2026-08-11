using NurSite.Application.DTOs;

namespace NurSite.Application.Interfaces;

public interface IOccasionService
{
    /// <summary>مناسبت‌هایی که در بازه داده‌شده می‌افتند، مرتب بر اساس تاریخ.</summary>
    Task<IReadOnlyList<OccasionOccurrence>> GetInRangeAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>مناسبت‌های پیش رو از امروز به بعد.</summary>
    Task<IReadOnlyList<OccasionOccurrence>> GetUpcomingAsync(
        int take = 6, int withinDays = 120, CancellationToken ct = default);
}