/*
 * کمک‌های صفحات ورود.
 * همه رویدادها به document واگذار می‌شوند تا مهم نباشد اسکریپت
 * زودتر از عناصر لود شده یا دیرتر.
 */
(function () {

    /* ---------- ۱) تبدیل ارقام فارسی و عربی به لاتین ---------- */
    function toLatinDigits(value) {
        var out = '';
        for (var i = 0; i < value.length; i++) {
            var c = value.charCodeAt(i);
            if (c >= 0x06F0 && c <= 0x06F9) out += String.fromCharCode(c - 0x06F0 + 48);
            else if (c >= 0x0660 && c <= 0x0669) out += String.fromCharCode(c - 0x0660 + 48);
            else out += value[i];
        }
        return out;
    }

    document.addEventListener('input', function (e) {
        var el = e.target;
        if (!el || !el.hasAttribute || !el.hasAttribute('data-persian-digits')) return;

        var converted = toLatinDigits(el.value);
        if (converted === el.value) return;

        var pos = el.selectionStart;
        el.value = converted;
        try { el.setSelectionRange(pos, pos); } catch (err) { /* بعضی نوع‌های input اجازه نمی‌دهند */ }
    });

    /* ---------- ۲) نمایش و پنهان کردن رمز ---------- */
    document.addEventListener('click', function (e) {
        /* کلیک ممکن است روی svg یا path داخل دکمه بیفتد، پس closest لازم است */
        var btn = e.target.closest ? e.target.closest('[data-toggle-password]') : null;
        if (!btn) return;

        e.preventDefault();

        /* اول با شناسه صریح، بعد به عنوان جایگزین در همان ظرف بگرد */
        var targetId = btn.getAttribute('data-toggle-password');
        var input = targetId ? document.getElementById(targetId) : null;

        if (!input) {
            var wrap = btn.closest('.input-wrap');
            input = wrap ? wrap.querySelector('input[type="password"], input[type="text"]') : null;
        }
        if (!input) return;

        var show = input.type === 'password';
        input.type = show ? 'text' : 'password';

        btn.setAttribute('aria-pressed', String(show));
        btn.setAttribute('aria-label', show ? 'پنهان کردن رمز عبور' : 'نمایش رمز عبور');

        /* آیکون را بین چشم باز و چشم خط‌خورده عوض کن */
        var open = btn.querySelector('[data-icon-open]');
        var closed = btn.querySelector('[data-icon-closed]');
        if (open && closed) {
            open.style.display = show ? 'none' : '';
            closed.style.display = show ? '' : 'none';
        }

        input.focus();
        var len = input.value.length;
        try { input.setSelectionRange(len, len); } catch (err) { }
    });

    /* ---------- ۳) هشدار Caps Lock ---------- */
    function checkCaps(e) {
        if (!e.target || e.target.type !== 'password' && e.target.type !== 'text') return;
        if (!e.target.closest || !e.target.closest('.field')) return;

        var hint = document.querySelector('[data-caps-hint]');
        if (!hint || typeof e.getModifierState !== 'function') return;

        hint.hidden = !e.getModifierState('CapsLock');
    }

    document.addEventListener('keydown', checkCaps);
    document.addEventListener('keyup', checkCaps);
    document.addEventListener('focusout', function (e) {
        if (!e.target || e.target.tagName !== 'INPUT') return;
        var hint = document.querySelector('[data-caps-hint]');
        if (hint) hint.hidden = true;
    });

    /* ---------- ۴) جلوگیری از ارسال دوباره فرم ---------- */
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form.classList || !form.classList.contains('auth-form')) return;

        var btn = form.querySelector('button[type="submit"]');
        if (!btn) return;

        /* در تیک بعدی غیرفعال کن تا مقدار دکمه همراه فرم ارسال شود */
        setTimeout(function () {
            btn.disabled = true;
            btn.dataset.originalText = btn.textContent;
            btn.textContent = 'لطفاً صبر کنید…';
        }, 0);
    });
})();