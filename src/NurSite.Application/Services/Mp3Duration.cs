namespace NurSite.Application.Services;

/// <summary>
/// خواندن مدت یک فایل mp3 از روی خود فایل، بدون کتابخانه بیرونی.
///
/// چرا دستی؟ چون تنها چیزی که لازم داریم یک عدد است و کتابخانه‌های
/// صوتی برای این کار سنگین‌اند و اغلب به کد بومی وابسته‌اند.
///
/// روش: هدر تگ ID3 رد می‌شود، اولین فریم معتبر پیدا می‌شود، و اگر
/// فریم هدر Xing یا VBRI داشته باشد تعداد فریم‌ها از آن خوانده می‌شود
/// (دقیق، حتی برای فایل با بیت‌ریت متغیر). اگر نداشت، از روی بیت‌ریت
/// و حجم تخمین زده می‌شود که برای فایل با بیت‌ریت ثابت دقیق است.
///
/// اگر فایل خراب یا از فرمت دیگری باشد، null برمی‌گرداند تا کاربر
/// خودش مدت را وارد کند — هیچ‌وقت استثنا پرتاب نمی‌کند.
/// </summary>
public static class Mp3Duration
{
    // نرخ نمونه‌برداری بر اساس نسخه MPEG و اندیس داخل هدر
    private static readonly int[][] SampleRates =
    [
        [11025, 12000, 8000],  // MPEG 2.5
        [0, 0, 0],             // رزرو
        [22050, 24000, 16000], // MPEG 2
        [44100, 48000, 32000]  // MPEG 1
    ];

    private static readonly int[] BitratesV1L3 =
        [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0];

    private static readonly int[] BitratesV2L3 =
        [0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0];

    /// <summary>مدت به ثانیه، یا null اگر خوانده نشد.</summary>
    public static int? Read(Stream stream)
    {
        try
        {
            if (!stream.CanSeek) return null;

            var length = stream.Length;
            if (length < 128) return null;

            stream.Position = 0;
            var start = SkipId3v2(stream);

            // تگ ID3v1 انتهای فایل جزء صوت نیست و باید از حجم کم شود
            var end = length - (HasId3v1(stream) ? 128 : 0);
            if (end <= start) return null;

            // گاهی چند بایت آشغال بین تگ و اولین فریم هست
            var header = new byte[4];
            var scanned = 0L;
            while (scanned < 8192 && start + scanned + 4 <= end)
            {
                stream.Position = start + scanned;
                stream.ReadExactly(header, 0, 4);

                var frame = FrameHeader.Parse(header);
                if (frame is not null)
                {
                    var frameStart = start + scanned;
                    var xingFrames = ReadXingFrameCount(stream, frameStart, frame);

                    if (xingFrames is > 0)
                    {
                        var seconds = (double)xingFrames.Value * frame.SamplesPerFrame / frame.SampleRate;
                        return (int)Math.Round(seconds);
                    }

                    // بیت‌ریت ثابت: حجم صوت تقسیم بر نرخ
                    var audioBytes = end - frameStart;
                    var estimate = audioBytes * 8.0 / (frame.BitrateKbps * 1000.0);
                    return estimate > 0 ? (int)Math.Round(estimate) : null;
                }

                scanned++;
            }

            return null;
        }
        catch
        {
            // خواندن مدت هرگز نباید جلوی آپلود را بگیرد
            return null;
        }
    }

    /// <summary>طول تگ ID3v2 ابتدای فایل. اگر تگ نداشت صفر.</summary>
    private static long SkipId3v2(Stream stream)
    {
        var head = new byte[10];
        stream.Position = 0;
        stream.ReadExactly(head, 0, 10);

        if (head[0] != 'I' || head[1] != 'D' || head[2] != '3') return 0;

        // اندازه در چهار بایت آخر است، هر بایت فقط ۷ بیت مفید دارد
        var size = (head[6] & 0x7F) << 21 |
                   (head[7] & 0x7F) << 14 |
                   (head[8] & 0x7F) << 7 |
                   (head[9] & 0x7F);

        return 10 + size;
    }

    private static bool HasId3v1(Stream stream)
    {
        if (stream.Length < 128) return false;

        var tag = new byte[3];
        stream.Position = stream.Length - 128;
        stream.ReadExactly(tag, 0, 3);
        return tag[0] == 'T' && tag[1] == 'A' && tag[2] == 'G';
    }

    /// <summary>
    /// تعداد فریم‌ها از هدر Xing یا VBRI، اگر فریم اول آن را داشته باشد.
    /// این تنها راه دقیق برای فایل با بیت‌ریت متغیر است.
    /// </summary>
    private static int? ReadXingFrameCount(Stream stream, long frameStart, FrameHeader frame)
    {
        var buffer = new byte[4];

        // Xing درست بعد از اطلاعات جانبی فریم می‌آید و اندازه آن
        // به نسخه MPEG و تک‌کاناله بودن بستگی دارد
        var xingOffset = frameStart + 4 + frame.SideInfoSize;
        if (xingOffset + 12 > stream.Length) return null;

        stream.Position = xingOffset;
        stream.ReadExactly(buffer, 0, 4);

        var tag = System.Text.Encoding.ASCII.GetString(buffer);
        if (tag is not ("Xing" or "Info"))
        {
            // VBRI همیشه ۳۲ بایت بعد از هدر فریم است
            var vbriOffset = frameStart + 36;
            if (vbriOffset + 18 > stream.Length) return null;

            stream.Position = vbriOffset;
            stream.ReadExactly(buffer, 0, 4);
            if (System.Text.Encoding.ASCII.GetString(buffer) != "VBRI") return null;

            stream.Position = vbriOffset + 14;
            stream.ReadExactly(buffer, 0, 4);
            return BigEndian(buffer);
        }

        stream.ReadExactly(buffer, 0, 4);
        var flags = BigEndian(buffer);

        // بیت اول یعنی تعداد فریم‌ها ثبت شده است
        if ((flags & 1) == 0) return null;

        stream.ReadExactly(buffer, 0, 4);
        return BigEndian(buffer);
    }

    private static int BigEndian(byte[] b) =>
        b[0] << 24 | b[1] << 16 | b[2] << 8 | b[3];

    /// <summary>هدر چهاربایتی یک فریم MPEG.</summary>
    private sealed record FrameHeader(int SampleRate, int BitrateKbps, int SamplesPerFrame, int SideInfoSize)
    {
        public static FrameHeader? Parse(byte[] h)
        {
            // یازده بیت اول باید یک باشند
            if (h[0] != 0xFF || (h[1] & 0xE0) != 0xE0) return null;

            var versionBits = (h[1] >> 3) & 0x03;
            var layerBits = (h[1] >> 1) & 0x03;
            if (versionBits == 1 || layerBits == 0) return null;

            // فقط لایه سه پشتیبانی می‌شود؛ سخنرانی‌ها همیشه همین‌اند
            if (layerBits != 1) return null;

            var bitrateIndex = (h[2] >> 4) & 0x0F;
            var sampleRateIndex = (h[2] >> 2) & 0x03;
            if (bitrateIndex is 0 or 15 || sampleRateIndex == 3) return null;

            var sampleRate = SampleRates[versionBits][sampleRateIndex];
            if (sampleRate == 0) return null;

            var isVersion1 = versionBits == 3;
            var bitrate = isVersion1 ? BitratesV1L3[bitrateIndex] : BitratesV2L3[bitrateIndex];
            if (bitrate == 0) return null;

            var isMono = ((h[3] >> 6) & 0x03) == 3;

            return new FrameHeader(
                SampleRate: sampleRate,
                BitrateKbps: bitrate,
                SamplesPerFrame: isVersion1 ? 1152 : 576,
                SideInfoSize: isVersion1 ? (isMono ? 17 : 32) : (isMono ? 9 : 17));
        }
    }
}