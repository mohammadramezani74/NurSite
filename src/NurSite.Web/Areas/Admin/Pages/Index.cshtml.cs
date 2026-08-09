using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurSite.Domain.Entities;
using NurSite.Domain.Enums;
using NurSite.Infrastructure.Persistence;

namespace NurSite.Web.Areas.Admin.Pages;

public class IndexModel(AppDbContext db) : PageModel
{
    // شمارنده‌های کارت‌های بالای داشبورد
    public int PendingQuestions { get; private set; }
    public int DraftArticles { get; private set; }
    public int UnreadMessages { get; private set; }
    public int UpcomingEventCount { get; private set; }

    // شمارنده‌های آمار کلی
    public int PublishedArticles { get; private set; }
    public int PublishedRulings { get; private set; }
    public int PublishedLectures { get; private set; }
    public int ActiveSubscribers { get; private set; }

    public IReadOnlyList<Event> NextEvents { get; private set; } = [];
    public IReadOnlyList<UserQuestion> LatestQuestions { get; private set; } = [];
    public IReadOnlyList<Article> RecentArticles { get; private set; } = [];

    /// <summary>مناسبت بعدی در تقویم قمری، اگر در سی روز آینده باشد.</summary>
    public (string Title, int DaysLeft)? NextOccasion { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        PendingQuestions = await db.UserQuestions
            .CountAsync(q => q.Status == QuestionStatus.New || q.Status == QuestionStatus.Assigned, ct);

        DraftArticles = await db.Articles
            .CountAsync(a => a.Status == PublishStatus.Draft || a.Status == PublishStatus.PendingReview, ct);

        UnreadMessages = await db.ContactMessages.CountAsync(m => !m.IsRead, ct);

        UpcomingEventCount = await db.Events
            .CountAsync(e => e.Status == PublishStatus.Published && e.StartsAtUtc >= now, ct);

        PublishedArticles = await db.Articles.CountAsync(a => a.Status == PublishStatus.Published, ct);
        PublishedRulings = await db.Rulings.CountAsync(r => r.Status == PublishStatus.Published, ct);
        PublishedLectures = await db.Lectures.CountAsync(l => l.Status == PublishStatus.Published, ct);
        ActiveSubscribers = await db.Subscribers.CountAsync(s => s.UnsubscribedAtUtc == null, ct);

        NextEvents = await db.Events.AsNoTracking()
            .Where(e => e.StartsAtUtc >= now)
            .OrderBy(e => e.StartsAtUtc)
            .Take(4)
            .ToListAsync(ct);

        LatestQuestions = await db.UserQuestions.AsNoTracking()
            .Where(q => q.Status == QuestionStatus.New || q.Status == QuestionStatus.Assigned)
            .OrderByDescending(q => q.CreatedAtUtc)
            .Take(5)
            .ToListAsync(ct);

        RecentArticles = await db.Articles.AsNoTracking()
            .OrderByDescending(a => a.UpdatedAtUtc ?? a.CreatedAtUtc)
            .Take(5)
            .ToListAsync(ct);

        NextOccasion = await FindNextOccasionAsync(ct);
    }

    /// <summary>
    /// نزدیک‌ترین مناسبت قمری. چون تاریخ قمری هر سال روی میلادی متفاوت می‌افتد،
    /// برای هر مناسبت تاریخ امسال و سال بعد قمری را حساب می‌کنیم و نزدیک‌ترین را برمی‌داریم.
    /// </summary>
    private async Task<(string, int)?> FindNextOccasionAsync(CancellationToken ct)
    {
        var occasions = await db.Occasions.AsNoTracking()
            .Where(o => o.IsActive)
            .ToListAsync(ct);

        if (occasions.Count == 0) return null;

        var hijri = new System.Globalization.UmAlQuraCalendar();
        var today = DateTime.UtcNow.Date;
        var hijriYear = hijri.GetYear(today);

        (string Title, int Days)? best = null;

        foreach (var o in occasions)
        {
            foreach (var year in new[] { hijriYear, hijriYear + 1 })
            {
                DateTime date;
                try
                {
                    date = hijri.ToDateTime(year, o.HijriMonth, o.HijriDay, 0, 0, 0, 0).Date;
                }
                catch (ArgumentOutOfRangeException)
                {
                    continue; // مثلاً روز ۳۰ در ماهی که ۲۹ روز دارد
                }

                var days = (date - today).Days;
                if (days < 0 || days > 30) continue;

                if (best is null || days < best.Value.Days)
                    best = (o.Title, days);
            }
        }

        return best;
    }
}