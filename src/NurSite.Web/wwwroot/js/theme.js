/* انتخاب پوسته — انتخاب کاربر در کوکی می‌ماند تا با رفرش از بین نرود */
(function () {
  var COOKIE = 'nur.theme';
  var VALID = ['lajvard', 'sabz', 'anabi'];

  function readCookie(name) {
    var m = document.cookie.match('(?:^|; )' + name + '=([^;]*)');
    return m ? decodeURIComponent(m[1]) : null;
  }
  function writeCookie(name, value) {
    var oneYear = 60 * 60 * 24 * 365;
    document.cookie = name + '=' + encodeURIComponent(value) +
      ';path=/;max-age=' + oneYear + ';samesite=lax';
  }

  function apply(theme) {
    if (VALID.indexOf(theme) === -1) return;
    document.documentElement.setAttribute('data-theme', theme);
    var meta = document.querySelector('meta[name="theme-color"]');
    if (meta) {
      meta.content = getComputedStyle(document.documentElement)
        .getPropertyValue('--ink').trim();
    }
    document.querySelectorAll('#palette button[data-theme]').forEach(function (b) {
      b.setAttribute('aria-pressed', String(b.dataset.theme === theme));
    });
  }

  /* پوسته سرور همیشه اولویت دارد وقتی مناسبت عزا فعال است */
  var forced = document.documentElement.dataset.themeForced === 'true';
  if (!forced) {
    var saved = readCookie(COOKIE);
    if (saved) apply(saved);
  }

  var palette = document.getElementById('palette');
  if (palette) {
    palette.addEventListener('click', function (e) {
      var btn = e.target.closest('button[data-theme]');
      if (!btn) return;
      apply(btn.dataset.theme);
      writeCookie(COOKIE, btn.dataset.theme);
    });
  }
})();
