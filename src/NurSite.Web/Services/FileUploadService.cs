using Microsoft.Extensions.Options;

namespace NurSite.Web.Services;

public sealed class UploadOptions
{
    public long MaxBytes { get; set; } = 3 * 1024 * 1024; // ۳ مگابایت
    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
}

public sealed record UploadResult(bool Ok, string? Path, string? Error);

/// <summary>
/// ذخیره تصاویر آپلودشده. مسیر بر اساس سال و ماه دسته‌بندی می‌شود تا
/// یک پوشه با هزاران فایل ساخته نشود.
/// </summary>
public sealed class FileUploadService(IWebHostEnvironment env, IOptions<UploadOptions> options)
{
    private readonly UploadOptions _opt = options.Value;

    public async Task<UploadResult> SaveImageAsync(IFormFile? file, string folder, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return new UploadResult(false, null, "فایلی انتخاب نشده است.");

        if (file.Length > _opt.MaxBytes)
            return new UploadResult(false, null,
                $"حجم فایل نباید بیشتر از {_opt.MaxBytes / 1024 / 1024} مگابایت باشد.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_opt.AllowedExtensions.Contains(ext))
            return new UploadResult(false, null, "فقط تصویر با پسوند jpg، png، webp یا gif پذیرفته می‌شود.");

        // به پسوند اعتماد نمی‌کنیم؛ چند بایت اول فایل باید امضای تصویر باشد
        if (!await LooksLikeImageAsync(file, ct))
            return new UploadResult(false, null, "محتوای فایل تصویر معتبر نیست.");

        var now = DateTime.UtcNow;
        var relativeDir = Path.Combine("uploads", folder, now.ToString("yyyy"), now.ToString("MM"));
        var absoluteDir = Path.Combine(env.WebRootPath, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        // نام فایل کاربر هرگز مستقیم استفاده نمی‌شود
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var absolutePath = Path.Combine(absoluteDir, fileName);

        await using (var stream = new FileStream(absolutePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        var webPath = "/" + Path.Combine(relativeDir, fileName).Replace('\\', '/');
        return new UploadResult(true, webPath, null);
    }

    public void Delete(string? webPath)
    {
        if (string.IsNullOrWhiteSpace(webPath) || !webPath.StartsWith("/uploads/")) return;

        var absolute = Path.Combine(env.WebRootPath, webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolute)) File.Delete(absolute);
    }

    /// <summary>بررسی امضای فایل (magic number) تا فایل اجرایی با پسوند jpg آپلود نشود.</summary>
    private static async Task<bool> LooksLikeImageAsync(IFormFile file, CancellationToken ct)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header, ct);
        if (read < 12) return false;

        // JPEG
        if (header[0] == 0xFF && header[1] == 0xD8) return true;
        // PNG
        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) return true;
        // GIF
        if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46) return true;
        // WEBP  →  RIFF....WEBP
        if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50) return true;

        return false;
    }
}