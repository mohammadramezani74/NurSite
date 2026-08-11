using NurSite.Domain.Enums;

namespace NurSite.Application.DTOs;

/// <summary>
/// یک مناسبت قمری که روی تاریخ میلادی مشخصی افتاده است.
/// چون مناسبت‌ها با روز و ماه قمری ذخیره می‌شوند، معادل میلادی‌شان
/// هر سال فرق می‌کند و باید محاسبه شود.
/// </summary>
public sealed record OccasionOccurrence(
    int OccasionId,
    string Title,
    string Slug,
    string? Description,
    OccasionKind Kind,
    bool IsPublicHoliday,
    DateOnly Date,
    int HijriMonth,
    int HijriDay)
{
    public int DaysFromToday => Date.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3.5)).DayNumber;
    public bool IsToday => DaysFromToday == 0;
    public bool IsPast => DaysFromToday < 0;
}