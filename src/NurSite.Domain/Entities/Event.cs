using NurSite.Domain.Common;
using NurSite.Domain.Enums;

namespace NurSite.Domain.Entities;

/// <summary>برنامه یا مراسم با تاریخ مشخص.</summary>
public class Event : BaseEntity, IAuditable, ISoftDelete, ISeoAware
{
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Summary { get; set; }
    public string? Body { get; set; }
    public string? CoverImagePath { get; set; }

    /// <summary>زمان شروع و پایان به UTC. تبدیل به شمسی فقط هنگام نمایش انجام می‌شود.</summary>
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    /// <summary>برای برنامه‌هایی مثل «پس از نماز مغرب» که ساعت دقیق ندارند.</summary>
    public string? TimeNote { get; set; }

    public string? LocationName { get; set; }
    public string? LocationAddress { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public int? SpeakerId { get; set; }
    public Speaker? Speaker { get; set; }

    public bool RegistrationRequired { get; set; }
    public int? Capacity { get; set; }
    public int RegisteredCount { get; set; }

    public PublishStatus Status { get; set; } = PublishStatus.Draft;
    public bool IsRecurringWeekly { get; set; }

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? OgImagePath { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedById { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedById { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
