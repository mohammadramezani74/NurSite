/* ثبت سرویس‌ورکر، دکمه نصب، و راهنمای نصب در iOS */
(function () {
    'use strict';

    /* ---------- ثبت سرویس‌ورکر ---------- */
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/sw.js').then(function (reg) {

                /* نسخه جدیدی آماده شد؟ به کاربر بگو، خودسرانه رفرش نکن */
                reg.addEventListener('updatefound', function () {
                    var incoming = reg.installing;
                    if (!incoming) return;

                    incoming.addEventListener('statechange', function () {
                        if (incoming.state !== 'installed' || !navigator.serviceWorker.controller) return;

                        var toast = document.getElementById('swToast');
                        if (!toast) return;

                        toast.classList.add('show');
                        toast.querySelector('button').addEventListener('click', function () {
                            incoming.postMessage('SKIP_WAITING');
                        });
                    });
                });
            }).catch(function (err) {
                console.warn('ثبت سرویس‌ورکر ناموفق بود:', err);
            });

            /* وقتی سرویس‌ورکر جدید کنترل را گرفت، یک بار رفرش کن */
            var refreshing = false;
            navigator.serviceWorker.addEventListener('controllerchange', function () {
                if (refreshing) return;
                refreshing = true;
                window.location.reload();
            });
        });
    }

    /* ---------- دکمه نصب ---------- */
    var panel = document.getElementById('installPanel');
    if (!panel) return;

    var button = panel.querySelector('[data-install]');
    var iosHelp = panel.querySelector('[data-ios-help]');
    var closeBtn = panel.querySelector('[data-install-close]');
    var DISMISS_KEY = 'nur:install-dismissed';

    function alreadyInstalled() {
        return window.matchMedia('(display-mode: standalone)').matches
            || window.navigator.standalone === true;
    }

    function dismissed() {
        try {
            var until = parseInt(window.localStorage.getItem(DISMISS_KEY), 10);
            return isFinite(until) && Date.now() < until;
        } catch (e) {
            return false;
        }
    }

    function dismiss() {
        panel.classList.remove('show');
        try {
            /* دو هفته دیگر دوباره بپرس. کسی که «نه» گفته را هر بار
               نباید با همان پیشنهاد غافلگیر کرد. */
            window.localStorage.setItem(DISMISS_KEY, String(Date.now() + 14 * 864e5));
        } catch (e) { /* حالت ناشناس مرورگر */ }
    }

    if (closeBtn) closeBtn.addEventListener('click', dismiss);

    if (alreadyInstalled() || dismissed()) return;

    /* اندروید و دسکتاپ: مرورگر خودش خبر می‌دهد که نصب ممکن است.
       رویداد را نگه می‌داریم تا کاربر روی دکمه ما بزند. */
    var deferred = null;

    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        deferred = e;

        panel.classList.add('show');
        if (button) button.hidden = false;
    });

    if (button) {
        button.addEventListener('click', function () {
            if (!deferred) return;

            deferred.prompt();
            deferred.userChoice.then(function () {
                /* چه نصب کند چه نه، این رویداد یک‌بارمصرف است */
                deferred = null;
                panel.classList.remove('show');
            });
        });
    }

    window.addEventListener('appinstalled', function () {
        panel.classList.remove('show');
        try { window.localStorage.removeItem(DISMISS_KEY); } catch (e) { }
    });

    /* iOS اصلاً رویداد نصب ندارد و دکمه نصب هم ندارد؛ تنها راهش
       «افزودن به صفحه اصلی» از منوی اشتراک‌گذاری سافاری است.
       پس به‌جای دکمه، راهنما نشان می‌دهیم. */
    var isIos = /iphone|ipad|ipod/i.test(window.navigator.userAgent);
    var isSafari = /safari/i.test(window.navigator.userAgent)
        && !/crios|fxios|edgios/i.test(window.navigator.userAgent);

    if (isIos && isSafari && iosHelp) {
        /* کمی صبر می‌کنیم تا کاربر اول با صفحه روبه‌رو شود */
        window.setTimeout(function () {
            iosHelp.hidden = false;
            panel.classList.add('show');
        }, 4000);
    }
})();