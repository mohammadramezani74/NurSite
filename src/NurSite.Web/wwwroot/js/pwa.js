/* ثبت سرویس‌ورکر و اطلاع‌رسانی نسخه جدید */
(function () {
  if (!('serviceWorker' in navigator)) return;

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
})();
