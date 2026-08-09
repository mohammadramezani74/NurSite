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
        public const string View    = "articles.view";
        public const string Create  = "articles.create";
        public const string Edit    = "articles.edit";
        public const string Delete  = "articles.delete";
        public const string Publish = "articles.publish";
    }

    public static class Rulings
    {
        public const string View    = "rulings.view";
        public const string Answer  = "rulings.answer";
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

    public static IEnumerable<string> All()
    {
        yield return Articles.View;    yield return Articles.Create;
        yield return Articles.Edit;    yield return Articles.Delete;
        yield return Articles.Publish;
        yield return Rulings.View;     yield return Rulings.Answer;
        yield return Rulings.Publish;
        yield return Media.Manage;     yield return Events.Manage;
        yield return Users.Manage;     yield return Settings.Manage;
    }
}
