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

/// <summary>یک تاریخ قمری، با نشانه اینکه از تقویم رسمی آمده یا محاسبه تقریبی.</summary>
public sealed record HijriDate(int Year, int Month, int Day, bool FromOfficialTable)
{
    private static readonly string[] MonthNames =
    {
        "محرم","صفر","ربیع‌الأول","ربیع‌الثانی","جمادی‌الأول","جمادی‌الثانی",
        "رجب","شعبان","رمضان","شوال","ذی‌القعده","ذی‌الحجه"
    };

    public string MonthName => MonthNames[Month - 1];
    public override string ToString() => $"{Day} {MonthName} {Year}";
}