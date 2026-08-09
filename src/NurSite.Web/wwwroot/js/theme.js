/*
 * انتخاب پوسته.
 * این فایل عمداً در <head> و بدون defer لود می‌شود تا رنگ پیش از رسم صفحه اعمال شود.
 * به همین دلیل هنگام اجرا هنوز <body> وجود ندارد — پس شنونده کلیک روی document
 * بسته می‌شود، نه روی خود دکمه‌ها.
 */
(function () {
    var COOKIE = 'nur.theme';
    var VALID = ['lajvard', 'sabz', 'anabi'];
    var root = document.documentElement;

    function readCookie(name) {
        var m = document.cookie.match('(?:^|; )' + name + '=([^;]*)');
        return m ? decodeURIComponent(m[1]) : null;
    }

    function writeCookie(name, value) {
        document.cookie = name + '=' + encodeURIComponent(value) +
            ';path=/;max-age=' + (60 * 60 * 24 * 365) + ';samesite=lax';
    }

    function syncButtons(theme) {
        var buttons = document.querySelectorAll('#palette button[data-theme]');
        for (var i = 0; i < buttons.length; i++) {
            buttons[i].setAttribute('aria-pressed', String(buttons[i].dataset.theme === theme));
        }
    }

    function apply(theme, persist) {
        if (VALID.indexOf(theme) === -1) return;

        root.setAttribute('data-theme', theme);

        var meta = document.querySelector('meta[name="theme-color"]');
        if (meta) {
            meta.content = getComputedStyle(root).getPropertyValue('--ink').trim();
        }

        syncButtons(theme);
        if (persist) writeCookie(COOKIE, theme);
    }

    /* پوسته اجباری مناسبت‌ها بر انتخاب کاربر مقدم است */
    var forced = root.dataset.themeForced === 'true';

    if (!forced) {
        var saved = readCookie(COOKIE);
        if (saved && saved !== root.getAttribute('data-theme')) {
            apply(saved, false);
        }
    }

    /* واگذاری رویداد به document — چون هنگام اجرای این اسکریپت دکمه‌ها هنوز ساخته نشده‌اند */
    document.addEventListener('click', function (e) {
        if (forced) return;
        var btn = e.target.closest ? e.target.closest('#palette button[data-theme]') : null;
        if (!btn) return;
        apply(btn.dataset.theme, true);
    });

    /* وقتی صفحه آماده شد، وضعیت دکمه‌ها را با پوسته فعلی هماهنگ کن */
    document.addEventListener('DOMContentLoaded', function () {
        syncButtons(root.getAttribute('data-theme'));
    });
})();