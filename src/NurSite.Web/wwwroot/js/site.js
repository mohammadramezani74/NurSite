/* ===== اسلایدر آیات ===== */
(function () {
    var box = document.getElementById('ayat');
    var navEl = document.getElementById('ayatNav');
    if (!box || !navEl) return;

    var slides = Array.prototype.slice.call(box.querySelectorAll('.ayeh'));
    if (slides.length < 2) return;

    var i = 0, timer = null, DUR = 7000;
    var reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    slides.forEach(function (_, n) {
        var b = document.createElement('button');
        b.type = 'button';
        b.setAttribute('role', 'tab');
        b.setAttribute('aria-label', 'آیه ' + (n + 1));
        b.setAttribute('aria-selected', n === 0 ? 'true' : 'false');
        b.addEventListener('click', function () { go(n); restart(); });
        navEl.appendChild(b);
    });
    var dots = Array.prototype.slice.call(navEl.children);

    function go(n) {
        slides[i].classList.remove('on');
        dots[i].setAttribute('aria-selected', 'false');
        i = (n + slides.length) % slides.length;
        slides[i].classList.add('on');
        dots[i].setAttribute('aria-selected', 'true');
    }
    function start() { if (!reduced) timer = setInterval(function () { go(i + 1); }, DUR); }
    function stop() { clearInterval(timer); }
    function restart() { stop(); start(); }

    box.addEventListener('mouseenter', stop);
    box.addEventListener('mouseleave', start);
    navEl.addEventListener('mouseenter', stop);
    navEl.addEventListener('mouseleave', start);
    document.addEventListener('visibilitychange', function () {
        document.hidden ? stop() : restart();
    });

    var x0 = null;
    box.addEventListener('touchstart', function (e) { x0 = e.touches[0].clientX; stop(); }, { passive: true });
    box.addEventListener('touchend', function (e) {
        if (x0 === null) return;
        var d = e.changedTouches[0].clientX - x0;
        if (Math.abs(d) > 44) go(d > 0 ? i + 1 : i - 1);
        x0 = null; start();
    }, { passive: true });

    start();
})();

/* ===== آکاردئون احکام: هر بار فقط یکی باز باشد ===== */
document.querySelectorAll('.acc-item').forEach(function (d) {
    d.addEventListener('toggle', function () {
        if (!d.open) return;
        document.querySelectorAll('.acc-item').forEach(function (o) { if (o !== d) o.open = false; });
    });
});

/* ===== منوی موبایل ===== */
(function () {
    var burger = document.querySelector('.burger');
    var nav = document.querySelector('.nav');
    if (!burger || !nav) return;

    burger.setAttribute('aria-expanded', 'false');
    burger.addEventListener('click', function () {
        var open = nav.classList.toggle('is-open');
        burger.setAttribute('aria-expanded', String(open));
        burger.setAttribute('aria-label', open ? 'بستن فهرست' : 'باز کردن فهرست');
    });

    nav.addEventListener('click', function (e) {
        if (e.target.tagName !== 'A') return;
        nav.classList.remove('is-open');
        burger.setAttribute('aria-expanded', 'false');
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && nav.classList.contains('is-open')) {
            nav.classList.remove('is-open');
            burger.setAttribute('aria-expanded', 'false');
            burger.focus();
        }
    });
})();

/* ===== کپی نشانی مقاله ===== */
document.addEventListener('click', function (e) {
    var btn = e.target.closest ? e.target.closest('.copy-link') : null;
    if (!btn || !btn.dataset.copy) return;

    var done = function () {
        var original = btn.textContent;
        btn.textContent = 'کپی شد';
        btn.classList.add('done');
        setTimeout(function () {
            btn.textContent = original;
            btn.classList.remove('done');
        }, 2000);
    };

    if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(btn.dataset.copy).then(done);
        return;
    }

    /* جایگزین برای مرورگرهای قدیمی یا اتصال بدون https */
    var input = document.createElement('input');
    input.value = btn.dataset.copy;
    document.body.appendChild(input);
    input.select();
    try { document.execCommand('copy'); done(); } catch (err) { }
    document.body.removeChild(input);
});