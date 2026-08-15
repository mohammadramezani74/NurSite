/* سرویس‌ورکر — نسخه با هر انتشار باید عوض شود */
const VERSION = 'v1.1.0';
const STATIC_CACHE = `nur-static-${VERSION}`;
const PAGES_CACHE  = `nur-pages-${VERSION}`;
const ASSETS_CACHE = `nur-assets-${VERSION}`;

/* صفحه‌ای که در نبود شبکه نشان داده می‌شود. اگر این یکی کش نشود،
   کل ماجرای آفلاین بی‌معنا می‌شود، پس جدا و اجباری گرفته می‌شود. */
const OFFLINE_URL = '/offline';

/* بقیه چیزها بهتر است از پیش کش شوند ولی حیاتی نیستند */
const PRECACHE = [
  '/css/site.css',
  '/js/site.js',
  '/js/theme.js',
  '/fonts/Vazirmatn-Regular.woff2',
  '/fonts/Vazirmatn-Bold.woff2',
  '/icons/icon-192.png',
  '/manifest.webmanifest'
];

/* ناوبری بیش از این منتظر شبکه نمی‌ماند و سراغ کش می‌رود */
const NAVIGATION_TIMEOUT = 8000;

self.addEventListener('install', (event) => {
  event.waitUntil((async () => {
    const cache = await caches.open(STATIC_CACHE);

    /* cache.addAll اگر یک فایل ۴۰۴ بدهد کل نصب را می‌اندازد. یکی‌یکی
       می‌گیریم تا یک فونت جاافتاده، سرویس‌ورکر را برای همیشه از کار نیندازد. */
    await Promise.all(PRECACHE.map(async (url) => {
      try {
        await cache.add(new Request(url, { cache: 'reload' }));
      } catch (err) {
        console.warn('[sw] پیش‌کش نشد:', url, err);
      }
    }));

    /* این یکی استثناست: بدون صفحه آفلاین، حالت آفلاین کار نمی‌کند */
    await cache.add(new Request(OFFLINE_URL, { cache: 'reload' }));

    await self.skipWaiting();
  })());
});

self.addEventListener('activate', (event) => {
  event.waitUntil((async () => {
    const keys = await caches.keys();
    await Promise.all(
      keys.filter((k) => k.startsWith('nur-') && !k.endsWith(VERSION))
          .map((k) => caches.delete(k))
    );

    /* پاسخ ناوبری را از کش مرورگر بده تا بارگذاری اول سریع‌تر شود */
    if (self.registration.navigationPreload) {
      await self.registration.navigationPreload.enable();
    }

    await self.clients.claim();
  })());
});

/* پیام از صفحه: کاربر روی «به‌روزرسانی» زد */
self.addEventListener('message', (event) => {
  if (event.data === 'SKIP_WAITING') self.skipWaiting();
});

function isAdminOrAuth(url) {
  return url.pathname.startsWith('/admin')
      || url.pathname.startsWith('/Identity')
      || url.pathname.startsWith('/vorood')
      || url.pathname.startsWith('/khorooj');
}

/* فایل‌های سنگین رسانه‌ای کش نمی‌شوند؛ یک سخنرانی چهل مگابایتی
   کل سهم ذخیره‌سازی مرورگر را می‌بلعد */
function isHeavyMedia(url) {
  return /\.(?:mp3|mp4|webm|m4a|ogg|wav)$/i.test(url.pathname)
      || url.pathname.startsWith('/danlod');
}

self.addEventListener('fetch', (event) => {
  const req = event.request;
  const url = new URL(req.url);

  /* فقط GET همان دامنه کش می‌شود */
  if (req.method !== 'GET' || url.origin !== self.location.origin) return;

  /* پنل ادمین، صفحات ورود و فایل‌های سنگین دست‌نخورده رد می‌شوند */
  if (isAdminOrAuth(url) || isHeavyMedia(url)) return;

  /* فونت، استایل، اسکریپت، تصویر — اول کش */
  if (/\.(?:woff2|css|js|png|jpg|jpeg|webp|svg|ico)$/.test(url.pathname)) {
    event.respondWith(cacheFirst(req, ASSETS_CACHE));
    return;
  }

  if (req.mode === 'navigate') {
    event.respondWith(navigate(event));
  }
});

async function cacheFirst(req, cacheName) {
  const cached = await caches.match(req);
  if (cached) return cached;

  try {
    const res = await fetch(req);
    if (res.ok) (await caches.open(cacheName)).put(req, res.clone());
    return res;
  } catch {
    return Response.error();
  }
}

/* شبکه با مهلت؛ اگر دیر کرد یا نشد، کش، و در نهایت صفحه آفلاین.
   بدون مهلت، یک شبکه کند یعنی صفحه‌ای که تا ابد می‌چرخد. */
async function navigate(event) {
  const req = event.request;
  const cache = await caches.open(PAGES_CACHE);

  try {
    const preloaded = await event.preloadResponse;
    if (preloaded) {
      if (preloaded.ok) cache.put(req, preloaded.clone());
      return preloaded;
    }

    const res = await withTimeout(fetch(req), NAVIGATION_TIMEOUT);
    if (res.ok) cache.put(req, res.clone());
    return res;
  } catch {
    const cached = await cache.match(req);
    if (cached) return cached;

    const offline = await caches.match(OFFLINE_URL);

    /* هیچ‌وقت undefined برنمی‌گردانیم — respondWith با مقدار تهی
       درخواست را می‌شکند و کاربر خطای مبهم مرورگر را می‌بیند */
    return offline || new Response(
      '<!doctype html><html lang="fa" dir="rtl"><meta charset="utf-8">' +
      '<title>آفلاین</title><body style="font-family:system-ui;text-align:center;padding:3rem">' +
      '<h1>اتصال اینترنت برقرار نیست</h1><p>پس از وصل شدن، دوباره تلاش کنید.</p>',
      { status: 503, headers: { 'Content-Type': 'text/html; charset=utf-8' } }
    );
  }
}

function withTimeout(promise, ms) {
  return Promise.race([
    promise,
    new Promise((_, reject) => setTimeout(() => reject(new Error('timeout')), ms))
  ]);
}