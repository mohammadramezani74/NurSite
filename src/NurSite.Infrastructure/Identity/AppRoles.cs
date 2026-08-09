namespace NurSite.Infrastructure.Identity;

/// <summary>نام نقش‌ها در یک جا، تا رشته جادویی در کد پخش نشود.</summary>
public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin      = "Admin";
    public const string Editor     = "Editor";
    public const string Author     = "Author";
    public const string Mufti      = "Mufti";
    public const string Member     = "Member";

    public static readonly (string Name, string Display, string Description)[] All =
    {
        (SuperAdmin, "مدیر ارشد", "دسترسی کامل به همه بخش‌ها، مدیریت کاربران و تنظیمات"),
        (Admin,      "مدیر",      "مدیریت کل محتوای سایت"),
        (Editor,     "ویراستار",  "تأیید و ویرایش محتوای تولیدشده توسط دیگران"),
        (Author,     "نویسنده",   "نوشتن و ویرایش محتوای خودش"),
        (Mufti,      "پاسخگوی شرعی", "پاسخ به پرسش‌های شرعی کاربران"),
        (Member,     "کاربر",     "کاربر عادی سایت")
    };
}
