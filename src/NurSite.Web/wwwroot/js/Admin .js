/* نوار کناری پنل روی موبایل */
(function () {
    var side = document.getElementById('adminSide');
    var burger = document.getElementById('adminBurger');
    var overlay = document.getElementById('adminOverlay');
    if (!side || !burger || !overlay) return;

    function open() {
        side.classList.add('is-open');
        overlay.hidden = false;
        burger.setAttribute('aria-expanded', 'true');
        burger.setAttribute('aria-label', 'بستن فهرست');
        document.body.style.overflow = 'hidden';
    }

    function close() {
        side.classList.remove('is-open');
        overlay.hidden = true;
        burger.setAttribute('aria-expanded', 'false');
        burger.setAttribute('aria-label', 'باز کردن فهرست');
        document.body.style.overflow = '';
    }

    burger.addEventListener('click', function () {
        side.classList.contains('is-open') ? close() : open();
    });

    overlay.addEventListener('click', close);

    /* با کلیک روی هر لینک فهرست بسته شود */
    side.addEventListener('click', function (e) {
        if (e.target.closest('a') && side.classList.contains('is-open')) close();
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && side.classList.contains('is-open')) {
            close();
            burger.focus();
        }
    });

    /* اگر پنجره بزرگ شد، حالت موبایل را پاک کن */
    window.addEventListener('resize', function () {
        if (window.innerWidth > 900 && side.classList.contains('is-open')) close();
    });
})();