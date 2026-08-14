using Microsoft.Extensions.Options;
using NurSite.Application.Services;

namespace NurSite.Web.Services;

public sealed class UploadOptions
{
    public long MaxBytes { get; set; } = 3 * 1024 * 1024; // ۳ مگابایت
    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    /// <summary>سقف و پسوندهای صوت جدا از تصویرند چون اندازه‌شان اصلاً قابل مقایسه نیست.</summary>
    public AudioOptions Audio { get; set; } = new();

    /// <summary>ویدیو از همه سنگین‌تر است و سقف خودش را دارد.</summary>
    public VideoOptions Video { get; set; } = new();
}

public sealed class AudioOptions
{
    public long MaxBytes { get; set; } = 60 * 1024 * 1024; // ۶۰ مگابایت
    public string[] AllowedExtensions { get; set; } = [".mp3"];
}

public sealed class VideoOptions
{
    public long MaxBytes { get; set; } = 100 * 1024 * 1024; // ۱۰۰ مگابایت
    public string[] AllowedExtensions { get; set; } = [".mp4", ".webm"];
}

/// <summary>
/// نتیجه آپلود تصویر. ابعاد اختیاری‌اند و اگر خوانده نشوند صفر می‌مانند —
/// فراخوان‌های قدیمی که فقط Ok و Path را می‌خواهند دست‌نخورده کار می‌کنند.
/// </summary>
public sealed record UploadResult(
    bool Ok,
    string? Path,
    string? Error,
    int Width = 0,
    int Height = 0,
    long SizeBytes = 0);

public sealed record VideoUploadResult(bool Ok, string? Path, long SizeBytes, string? Error);

/// <summary>نتیجه آپلود صوت. مدت و حجم برای نمایش و برای نشانه‌گذاری ساختاریافته لازم‌اند.</summary>
public sealed record AudioUploadResult(
    bool Ok,
    string? Path,
    long SizeBytes,
    int? DurationSeconds,
    string? Error);

/// <summary>
/// ذخیره فایل‌های آپلودشده. مسیر بر اساس سال و ماه دسته‌بندی می‌شود تا
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

        var saved = await SaveAsync(file, folder, ext, ct);

        // ابعاد از خود فایل ذخیره‌شده خوانده می‌شود، نه از استریم آپلود،
        // چون استریم آپلود همیشه قابل جابه‌جایی نیست و خواندن هدر تصویر
        // نیاز به عقب و جلو رفتن دارد
        var size = ReadImageSize(saved.AbsolutePath);

        return new UploadResult(
            true, saved.WebPath, null,
            size?.Width ?? 0, size?.Height ?? 0, file.Length);
    }

    /// <summary>
    /// ذخیره ویدیوی کوتاه. سقفش از صوت هم بالاتر است، پس پیش از هر چیز
    /// باید مطمئن شد سقف بدنه درخواست در Program.cs همین اندازه هست.
    /// </summary>
    public async Task<VideoUploadResult> SaveVideoAsync(IFormFile? file, string folder, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return new VideoUploadResult(false, null, 0, "فایلی انتخاب نشده است.");

        var limit = _opt.Video.MaxBytes;
        if (file.Length > limit)
            return new VideoUploadResult(false, null, 0,
                $"حجم ویدیو نباید بیشتر از {limit / 1024 / 1024} مگابایت باشد.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_opt.Video.AllowedExtensions.Contains(ext))
        {
            var allowed = string.Join("، ", _opt.Video.AllowedExtensions.Select(e => e.TrimStart('.')));
            return new VideoUploadResult(false, null, 0, $"فقط ویدیو با پسوند {allowed} پذیرفته می‌شود.");
        }

        if (!await LooksLikeVideoAsync(file, ct))
            return new VideoUploadResult(false, null, 0, "محتوای فایل ویدیو معتبر نیست.");

        var saved = await SaveAsync(file, folder, ext, ct);
        return new VideoUploadResult(true, saved.WebPath, file.Length, null);
    }

    private static ImageSize.Size? ReadImageSize(string absolutePath)
    {
        using var stream = File.OpenRead(absolutePath);
        return ImageSize.Read(stream);
    }

    /// <summary>
    /// ذخیره فایل صوتی. مدت زمان بعد از ذخیره از روی خود فایل خوانده می‌شود،
    /// نه از استریم آپلود — چون استریم آپلود همیشه قابل جابه‌جایی نیست و
    /// خواندن هدر mp3 نیاز به عقب و جلو رفتن دارد.
    /// </summary>
    public async Task<AudioUploadResult> SaveAudioAsync(IFormFile? file, string folder, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return new AudioUploadResult(false, null, 0, null, "فایلی انتخاب نشده است.");

        var limit = _opt.Audio.MaxBytes;
        if (file.Length > limit)
            return new AudioUploadResult(false, null, 0, null,
                $"حجم فایل صوتی نباید بیشتر از {limit / 1024 / 1024} مگابایت باشد.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_opt.Audio.AllowedExtensions.Contains(ext))
        {
            var allowed = string.Join("، ", _opt.Audio.AllowedExtensions.Select(e => e.TrimStart('.')));
            return new AudioUploadResult(false, null, 0, null, $"فقط فایل صوتی با پسوند {allowed} پذیرفته می‌شود.");
        }

        if (!await LooksLikeAudioAsync(file, ct))
            return new AudioUploadResult(false, null, 0, null, "محتوای فایل صوتی معتبر نیست.");

        var saved = await SaveAsync(file, folder, ext, ct);

        int? duration = null;
        if (ext == ".mp3")
        {
            await using var stream = File.OpenRead(saved.AbsolutePath);
            duration = Mp3Duration.Read(stream);
        }

        return new AudioUploadResult(true, saved.WebPath, file.Length, duration, null);
    }

    public void Delete(string? webPath)
    {
        if (string.IsNullOrWhiteSpace(webPath) || !webPath.StartsWith("/uploads/")) return;

        var absolute = Path.Combine(env.WebRootPath, webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolute)) File.Delete(absolute);
    }

    private async Task<(string WebPath, string AbsolutePath)> SaveAsync(
        IFormFile file, string folder, string ext, CancellationToken ct)
    {
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
        return (webPath, absolutePath);
    }

    /// <summary>بررسی امضای فایل (magic number) تا فایل اجرایی با پسوند jpg آپلود نشود.</summary>
    private static async Task<bool> LooksLikeImageAsync(IFormFile file, CancellationToken ct)
    {
        var header = await ReadHeaderAsync(file, 12, ct);
        if (header is null) return false;

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

    private static async Task<bool> LooksLikeAudioAsync(IFormFile file, CancellationToken ct)
    {
        var header = await ReadHeaderAsync(file, 12, ct);
        if (header is null) return false;

        // MP3 با تگ ID3 شروع می‌شود…
        if (header[0] == 'I' && header[1] == 'D' && header[2] == '3') return true;
        // …یا مستقیم با هدر فریم که یازده بیت اولش یک است
        if (header[0] == 0xFF && (header[1] & 0xE0) == 0xE0) return true;
        // M4A / MP4  →  ....ftyp
        if (header[4] == 'f' && header[5] == 't' && header[6] == 'y' && header[7] == 'p') return true;
        // OGG
        if (header[0] == 'O' && header[1] == 'g' && header[2] == 'g' && header[3] == 'S') return true;
        // WAV  →  RIFF....WAVE
        if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x41 && header[10] == 0x56 && header[11] == 0x45) return true;

        return false;
    }

    private static async Task<bool> LooksLikeVideoAsync(IFormFile file, CancellationToken ct)
    {
        var header = await ReadHeaderAsync(file, 12, ct);
        if (header is null) return false;

        // MP4 / MOV  →  ....ftyp
        if (header[4] == 'f' && header[5] == 't' && header[6] == 'y' && header[7] == 'p') return true;
        // WEBM / MKV  →  امضای ماتروسکا
        if (header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3) return true;

        return false;
    }

    private static async Task<byte[]?> ReadHeaderAsync(IFormFile file, int count, CancellationToken ct)
    {
        var header = new byte[count];
        await using var stream = file.OpenReadStream();

        var read = 0;
        while (read < count)
        {
            // یک بار خواندن ممکن است کمتر از خواسته برگرداند
            var n = await stream.ReadAsync(header.AsMemory(read, count - read), ct);
            if (n == 0) return null;
            read += n;
        }

        return header;
    }
}