namespace NurSite.Application.Interfaces;

public interface ISlugService
{
    /// <summary>عنوان را به اسلاگ تبدیل می‌کند. حروف فارسی حفظ می‌شوند.</summary>
    string Generate(string title);

    /// <summary>اگر اسلاگ تکراری باشد، پسوند عددی اضافه می‌کند.</summary>
    Task<string> GenerateUniqueAsync<TEntity>(string title, int? excludeId = null, CancellationToken ct = default);
}
