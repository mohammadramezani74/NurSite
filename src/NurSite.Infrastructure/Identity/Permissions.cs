namespace NurSite.Infrastructure.Identity;

/// <summary>
/// دسترسی‌ها به صورت Claim تعریف می‌شوند، نه نقش. این کار توسعه بعدی را
/// خیلی ساده‌تر می‌کند: افزودن نقش جدید نیازی به تغییر کد صفحات ندارد.
/// </summary>
public static class Permissions
{
    public const string ClaimType = "permission";

    public static class Articles
    {
        public const string View = "articles.view";
        public const string Create = "articles.create";
        public const string Edit = "articles.edit";
        public const string Delete = "articles.delete";
        public const string Publish = "articles.publish";
    }

    public static class Rulings
    {
        public const string View = "rulings.view";
        public const string Answer = "rulings.answer";
        public const string Publish = "rulings.publish";
    }

    public static class Media
    {
        public const string Manage = "media.manage";
    }

    public static class Events
    {
        public const string Manage = "events.manage";
    }

    public static class Users
    {
        public const string Manage = "users.manage";
    }

    public static class Settings
    {
        public const string Manage = "settings.manage";
    }

    /// <summary>دسترسی‌های پیش‌فرض هر نقش، هنگام مقداردهی اولیه دیتابیس اعمال می‌شود.</summary>
    public static IReadOnlyDictionary<string, string[]> DefaultsByRole { get; } =
        new Dictionary<string, string[]>
        {
            [AppRoles.SuperAdmin] = All().ToArray(),
            [AppRoles.Admin] = new[]
            {
                Articles.View, Articles.Create, Articles.Edit, Articles.Delete, Articles.Publish,
                Rulings.View, Rulings.Answer, Rulings.Publish,
                Media.Manage, Events.Manage
            },
            [AppRoles.Editor] = new[]
            {
                Articles.View, Articles.Edit, Articles.Publish, Rulings.View, Media.Manage
            },
            [AppRoles.Author] = new[]
            {
                Articles.View, Articles.Create, Articles.Edit
            },
            [AppRoles.Mufti] = new[]
            {
                Rulings.View, Rulings.Answer
            },
            [AppRoles.Member] = Array.Empty<string>()
        };

    /// <summary>
    /// نام فارسی و گروه هر دسترسی، برای نمایش در پنل نقش‌ها.
    /// اینجا نگه داشته می‌شود تا با افزودن دسترسی تازه، جای دیگری جا نماند.
    /// </summary>
    public static IReadOnlyDictionary<string, (string Group, string Title, string Hint)> Describe { get; } =
        new Dictionary<string, (string, string, string)>
        {
            [Articles.View] = ("مقالات", "دیدن فهرست", "بدون این، بخش مقالات در پنل دیده نمی‌شود"),
            [Articles.Create] = ("مقالات", "نوشتن مقاله تازه", ""),
            [Articles.Edit] = ("مقالات", "ویرایش مقاله", "شامل مقاله‌های دیگران هم می‌شود"),
            [Articles.Delete] = ("مقالات", "حذف مقاله", ""),
            [Articles.Publish] = ("مقالات", "انتشار و بازگرداندن به پیش‌نویس", ""),

            [Rulings.View] = ("احکام", "دیدن فهرست", "بدون این، بخش احکام در پنل دیده نمی‌شود"),
            [Rulings.Answer] = ("احکام", "پاسخ به پرسش شرعی", "شامل نوشتن و ویرایش حکم"),
            [Rulings.Publish] = ("احکام", "انتشار حکم", ""),

            [Media.Manage] = ("رسانه", "مدیریت صوت و گالری", "سخنرانی، مداحی، پوستر و کلیپ"),
            [Events.Manage] = ("برنامه‌ها", "مدیریت برنامه‌ها", "هنوز ساخته نشده است"),

            [Users.Manage] = ("سیستم", "مدیریت کاربران و نقش‌ها", "با این دسترسی می‌شود دسترسی بقیه را هم عوض کرد"),
            [Settings.Manage] = ("سیستم", "تنظیمات سایت", "شامل آیات، تقویم و مناسبت‌ها")
        };

    public static IEnumerable<string> All()
    {
        yield return Articles.View; yield return Articles.Create;
        yield return Articles.Edit; yield return Articles.Delete;
        yield return Articles.Publish;
        yield return Rulings.View; yield return Rulings.Answer;
        yield return Rulings.Publish;
        yield return Media.Manage; yield return Events.Manage;
        yield return Users.Manage; yield return Settings.Manage;
    }
}