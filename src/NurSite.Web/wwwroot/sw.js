/* سرویس‌ورکر — نسخه با هر انتشار باید عوض شود */
const VERSION = 'v1.0.0';
const STATIC_CACHE = `nur-static-${VERSION}`;
const PAGES_CACHE  = `nur-pages-${VERSION}`;
const ASSETS_CACHE = `nur-assets-${VERSION}`;

/* حداقل چیزی که باید آفلاین در دسترس باشد */
const PRECACHE = [
  '/offline',
  '/css/site.css',
  '/js/site.js',
  '/js/theme.js',
  '/fonts/Vazirmatn-Regular.woff2',
  '/fonts/Vazirmatn-Bold.woff2',
  '/icons/icon-192.png',
  '/manifest.webmanifest'
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(STATIC_CACHE)
      .then((cache) => cache.addAll(PRECACHE))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys()
      .then((keys) => Promise.all(
        keys.filter((k) => !k.endsWith(VERSION)).map((k) => caches.delete(k))
      ))
      .then(() => self.clients.claim())
  );
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

self.addEventListener('fetch', (event) => {
  const req = event.request;
  const url = new URL(req.url);

  /* فقط GET همان دامنه کش می‌شود */
  if (req.method !== 'GET' || url.origin !== self.location.origin) return;

  /* پنل ادمین و صفحات ورود هرگز کش نمی‌شوند */
  if (isAdminOrAuth(url)) return;

  /* فونت، استایل، اسکریپت، تصویر — اول کش */
  if (/\.(?:woff2|css|js|png|jpg|jpeg|webp|svg|ico)$/.test(url.pathname)) {
    event.respondWith(cacheFirst(req, ASSETS_CACHE));
    return;
  }

  /* اوقات شرعی — نمایش فوری از کش، به‌روزرسانی در پس‌زمینه */
  if (url.pathname.startsWith('/api/owqat')) {
    event.respondWith(staleWhileRevalidate(req, PAGES_CACHE));
    return;
  }

  /* صفحات — اول شبکه، اگر نبود کش، اگر نبود صفحه آفلاین */
  if (req.mode === 'navigate') {
    event.respondWith(networkFirst(req, PAGES_CACHE));
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
    return new Response('', { status: 504, statusText: 'offline' });
  }
}

async function networkFirst(req, cacheName) {
  try {
    const res = await fetch(req);
    if (res.ok) (await caches.open(cacheName)).put(req, res.clone());
    return res;
  } catch {
    const cached = await caches.match(req);
    return cached || caches.match('/offline');
  }
}

async function staleWhileRevalidate(req, cacheName) {
  const cache = await caches.open(cacheName);
  const cached = await cache.match(req);
  const network = fetch(req).then((res) => {
    if (res.ok) cache.put(req, res.clone());
    return res;
  }).catch(() => cached);
  return cached || network;
}

/* --- قلاب اعلان‌ها، فاز دوم فعال می‌شود ---
self.addEventListener('push', (event) => { ... });
self.addEventListener('notificationclick', (event) => { ... });
*/
