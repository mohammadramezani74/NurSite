namespace NurSite.Domain.Enums;

/// <summary>چه کسی می‌تواند فایل صوتی را دانلود کند.</summary>
public enum DownloadAccess
{
    /// <summary>همه، حتی مهمان.</summary>
    Everyone = 0,

    /// <summary>فقط کاربری که وارد شده است.</summary>
    SignedIn = 1,

    /// <summary>هیچ‌کس؛ فقط پخش آنلاین.</summary>
    Disabled = 2
}