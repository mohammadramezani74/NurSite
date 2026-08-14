namespace NurSite.Application.Services;

/// <summary>
/// خواندن عرض و ارتفاع تصویر از روی چند بایت اول فایل، بدون کتابخانه بیرونی.
///
/// چرا لازم است؟ چون اگر ابعاد را ندانیم نمی‌توانیم در HTML مقدار
/// width و height بگذاریم، و بدون آن‌ها مرورگر جای تصویر را از قبل
/// نگه نمی‌دارد و صفحه موقع بارگذاری می‌پرد.
///
/// چرا دستی؟ چون پروژه هیچ کتابخانه تصویری ندارد و برای گرفتن دو عدد،
/// اضافه‌کردن یکی از آن‌ها به کل وابستگی‌ها می‌ارزد نیست. فقط هدر خوانده
/// می‌شود، نه خود تصویر — پس حافظه هم درگیر نمی‌شود.
///
/// اگر فرمت ناشناخته یا فایل خراب باشد null برمی‌گرداند و هیچ‌وقت
/// استثنا پرتاب نمی‌کند.
/// </summary>
public static class ImageSize
{
    public readonly record struct Size(int Width, int Height);

    public static Size? Read(Stream stream)
    {
        try
        {
            if (!stream.CanSeek || stream.Length < 16) return null;

            stream.Position = 0;
            var head = new byte[32];
            stream.ReadExactly(head, 0, 32);

            // PNG
            if (head[0] == 0x89 && head[1] == 'P' && head[2] == 'N' && head[3] == 'G')
                return new Size(BigEndian32(head, 16), BigEndian32(head, 20));

            // GIF — ابعاد کوچک‌اندین و درست بعد از امضا
            if (head[0] == 'G' && head[1] == 'I' && head[2] == 'F')
                return new Size(head[6] | head[7] << 8, head[8] | head[9] << 8);

            // WEBP  →  RIFF....WEBP
            if (head[0] == 'R' && head[1] == 'I' && head[2] == 'F' && head[3] == 'F' &&
                head[8] == 'W' && head[9] == 'E' && head[10] == 'B' && head[11] == 'P')
                return ReadWebp(head);

            // JPEG
            if (head[0] == 0xFF && head[1] == 0xD8)
                return ReadJpeg(stream);

            return null;
        }
        catch
        {
            // ابعاد تصویر آنقدر مهم نیست که جلوی آپلود را بگیرد
            return null;
        }
    }

    /// <summary>
    /// وب‌پی سه قالب دارد و ابعاد در هر سه جای متفاوتی است.
    /// </summary>
    private static Size? ReadWebp(byte[] head)
    {
        var format = System.Text.Encoding.ASCII.GetString(head, 12, 4);

        switch (format)
        {
            // ساده و بدون افت کیفیت: ابعاد در چهارده بیت پس از کد شروع فریم
            case "VP8 ":
                if (head[23] != 0x9D || head[24] != 0x01 || head[25] != 0x2A) return null;
                return new Size(
                    (head[26] | head[27] << 8) & 0x3FFF,
                    (head[28] | head[29] << 8) & 0x3FFF);

            // بدون افت کیفیت: عرض و ارتفاع در بیت‌های فشرده و منهای یک
            case "VP8L":
                if (head[20] != 0x2F) return null;
                var bits = head[21] | head[22] << 8 | head[23] << 16 | head[24] << 24;
                return new Size((bits & 0x3FFF) + 1, (bits >> 14 & 0x3FFF) + 1);

            // گسترده: ابعاد بوم در سه بایت و منهای یک
            case "VP8X":
                return new Size(
                    (head[24] | head[25] << 8 | head[26] << 16) + 1,
                    (head[27] | head[28] << 8 | head[29] << 16) + 1);

            default:
                return null;
        }
    }

    /// <summary>
    /// جی‌پگ ابعاد را در هدر ندارد؛ باید بخش‌ها را یکی‌یکی رد کرد تا به
    /// بخش «شروع فریم» رسید. اندازه هر بخش در دو بایت اولش نوشته شده.
    /// </summary>
    private static Size? ReadJpeg(Stream stream)
    {
        stream.Position = 2;
        var buffer = new byte[4];

        while (stream.Position < stream.Length - 8)
        {
            // هر بخش با ۰xFF شروع می‌شود؛ گاهی چند ۰xFF پشت سر هم می‌آید
            int marker;
            do
            {
                marker = stream.ReadByte();
                if (marker < 0) return null;
            }
            while (marker != 0xFF);

            do
            {
                marker = stream.ReadByte();
                if (marker < 0) return null;
            }
            while (marker == 0xFF);

            // بخش‌های «شروع فریم» ابعاد را دارند. ۰xC4 و ۰xC8 و ۰xCC
            // با اینکه در همین بازه‌اند، جدول‌اند نه فریم.
            var isStartOfFrame =
                marker is >= 0xC0 and <= 0xCF &&
                marker is not (0xC4 or 0xC8 or 0xCC);

            if (isStartOfFrame)
            {
                // دو بایت طول، یک بایت دقت، بعد ارتفاع و عرض
                stream.ReadExactly(buffer, 0, 3);
                stream.ReadExactly(buffer, 0, 4);
                return new Size(buffer[2] << 8 | buffer[3], buffer[0] << 8 | buffer[1]);
            }

            // بخش‌های بدون داده، طول ندارند
            if (marker is 0xD8 or 0x01 || marker is >= 0xD0 and <= 0xD7) continue;

            stream.ReadExactly(buffer, 0, 2);
            var length = buffer[0] << 8 | buffer[1];
            if (length < 2) return null;

            stream.Position += length - 2;
        }

        return null;
    }

    private static int BigEndian32(byte[] b, int offset) =>
        b[offset] << 24 | b[offset + 1] << 16 | b[offset + 2] << 8 | b[offset + 3];
}