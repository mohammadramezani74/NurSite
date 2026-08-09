# مؤسسه فرهنگی نورالثقلین — سایت

سایت مذهبی با ASP.NET Core 9 Razor Pages، معماری لایه‌بندی‌شده، پشتیبانی کامل راست‌چین و PWA.

## پیش‌نیازها

- .NET 9 SDK
- SQL Server (LocalDB هم کافی است)

## راه‌اندازی

```bash
# ۱. رشته اتصال را در appsettings.Development.json تنظیم کنید

# ۲. ساخت مایگریشن اولیه
dotnet ef migrations add InitialCreate \
  --project src/NurSite.Infrastructure \
  --startup-project src/NurSite.Web

# ۳. اعمال روی دیتابیس
dotnet ef database update \
  --project src/NurSite.Infrastructure \
  --startup-project src/NurSite.Web

# ۴. اجرا
dotnet run --project src/NurSite.Web
```

در محیط توسعه، دیتابیس خودکار مقداردهی اولیه می‌شود:
نقش‌ها، دسترسی‌ها، شهرها، آیات هیرو، مناسبت‌های قمری و تنظیمات سایت.

**مدیر ارشد پیش‌فرض:** شماره `09120000000` — رمز در `appsettings.Development.json`.
پیش از انتشار حتماً عوضش کنید.

## ساختار

| لایه | مسئولیت |
|---|---|
| `NurSite.Domain` | موجودیت‌ها و قواعد کسب‌وکار. بدون هیچ وابستگی بیرونی |
| `NurSite.Application` | قراردادها (اینترفیس‌ها) و DTOها |
| `NurSite.Infrastructure` | EF Core، Identity، پیاده‌سازی سرویس‌ها |
| `NurSite.Web` | Razor Pages، TagHelperها، فایل‌های ایستا، PWA |

## کارهای باقی‌مانده پیش از اجرا

۱. **فونت‌ها** را در `wwwroot/fonts/` قرار دهید (فرمت woff2):
   Vazirmatn در وزن‌های ۳۰۰ تا ۸۰۰، Gulzar Regular، Amiri Bold.
   از مخزن رسمی وزیرمتن در گیت‌هاب قابل دریافت است.

۲. **آیکون‌های PWA** را در `wwwroot/icons/` بسازید:
   `icon-192.png`، `icon-512.png`، `maskable-192.png`، `maskable-512.png`.

۳. **صفحاتی که هنوز ساخته نشده‌اند** و در Layout به آن‌ها لینک داده شده:
   `/Vorood`، `/Khorooj`، `/Porsesh`، `/Owqat`، `/Maghalat`، `/Ahkam`،
   `/Barnameh`، `/Sokhanrani`، `/Gallery`، `/Monasebat`، `/Tamas`، `/Khata`.
   تا ساخته نشوند، `asp-page` در زمان اجرا خطا می‌دهد.

## نکات مهم

- تاریخ‌ها **همیشه** به UTC ذخیره می‌شوند. تبدیل به شمسی فقط هنگام نمایش انجام می‌شود.
- اسلاگ‌ها فارسی هستند؛ گوگل آدرس‌های فارسی را درست ایندکس می‌کند.
- حذف محتوا نرم است (`IsDeleted`)، پس فیلتر سراسری روی کوئری‌ها اعمال شده.
- پنل ادمین هرگز در سرویس‌ورکر کش نمی‌شود.
- نسخه سرویس‌ورکر (`VERSION` در `sw.js`) با هر انتشار باید عوض شود.
