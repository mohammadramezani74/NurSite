/* ویرایشگر مقاله: TinyMCE، ساخت اسلاگ، شمارنده‌ها و پیش‌نمایش نتیجه گوگل */
(function () {

    /* ---------- کمکی‌ها ---------- */
    function faDigits(value) {
        return String(value).replace(/[0-9]/g, function (d) {
            return String.fromCharCode(d.charCodeAt(0) + 0x06F0 - 48);
        });
    }

    var titleInput = document.getElementById('titleInput');
    var slugInput = document.getElementById('slugInput');
    var metaTitleInput = document.getElementById('metaTitleInput');
    var metaDescInput = document.getElementById('metaDescInput');

    /* ---------- ۱) ساخت خودکار اسلاگ از عنوان ---------- */
    function makeSlug(text) {
        return text
            .trim()
            .replace(/[يى]/g, 'ی').replace(/ك/g, 'ک')
            /* «آ» عمداً تبدیل نمی‌شود — حرف مستقل فارسی است */
            .replace(/[أإ]/g, 'ا').replace(/ة/g, 'ه')
            .replace(/[\s\u200C]+/g, '-')
            .replace(/[^\p{L}\p{N}-]+/gu, '-')
            .replace(/-{2,}/g, '-')
            .replace(/^-|-$/g, '')
            .toLowerCase();
    }

    if (titleInput && slugInput) {
        /* اگر کاربر خودش اسلاگ را دست‌کاری کرد، دیگر خودکار عوضش نکن */
        var slugTouched = slugInput.value.trim().length > 0;
        slugInput.addEventListener('input', function () { slugTouched = true; });

        titleInput.addEventListener('input', function () {
            if (!slugTouched) slugInput.value = makeSlug(titleInput.value);
            updateSerp();
        });
    }

    /* ---------- ۲) شمارنده کاراکتر ---------- */
    document.querySelectorAll('[data-counter]').forEach(function (el) {
        var out = document.getElementById(el.dataset.counter);
        if (!out) return;

        function update() {
            out.textContent = faDigits(el.value.length);
            var max = parseInt(el.getAttribute('maxlength'), 10);
            if (!max) return;
            var ratio = el.value.length / max;
            out.parentElement.classList.toggle('near', ratio > 0.9);
        }
        el.addEventListener('input', update);
        update();
    });

    /* ---------- ۳) پیش‌نمایش نتیجه گوگل ---------- */
    var serpTitle = document.getElementById('serpTitle');
    var serpDesc = document.getElementById('serpDesc');
    var serpUrl = document.getElementById('serpUrl');
    var baseUrl = serpUrl ? serpUrl.textContent.trim() : '';

    function updateSerp() {
        if (!serpTitle) return;

        var t = (metaTitleInput && metaTitleInput.value.trim())
            || (titleInput && titleInput.value.trim())
            || 'عنوان مقاله';

        var d = (metaDescInput && metaDescInput.value.trim())
            || 'توضیح متا اینجا نمایش داده می‌شود.';

        serpTitle.textContent = t;
        serpDesc.textContent = d;

        if (serpUrl && slugInput) {
            serpUrl.textContent = baseUrl + (slugInput.value.trim() || '…');
        }
    }

    [metaTitleInput, metaDescInput, slugInput].forEach(function (el) {
        if (el) el.addEventListener('input', updateSerp);
    });
    updateSerp();

    /* ---------- ۴) پیش‌نمایش تصویر پیش از آپلود ---------- */
    var coverFile = document.getElementById('CoverFile');
    var coverPreview = document.getElementById('coverPreview');
    if (coverFile && coverPreview) {
        coverFile.addEventListener('change', function () {
            var file = coverFile.files && coverFile.files[0];
            if (!file) return;

            var url = URL.createObjectURL(file);
            coverPreview.classList.remove('empty');
            coverPreview.innerHTML = '';
            var img = document.createElement('img');
            img.src = url;
            img.alt = '';
            img.onload = function () { URL.revokeObjectURL(url); };
            coverPreview.appendChild(img);
        });
    }

    /* ---------- ۵) ویرایشگر متن ---------- */
    if (typeof tinymce === 'undefined') {
        console.warn('TinyMCE لود نشد؛ ویرایشگر ساده نمایش داده می‌شود.');
        return;
    }

    tinymce.init({
        selector: '#bodyEditor',
        directionality: 'rtl',
        language: 'fa',
        height: 520,
        menubar: false,
        branding: false,
        promotion: false,
        plugins: 'lists link image table code autolink charmap searchreplace wordcount fullscreen',
        toolbar:
            'undo redo | blocks | bold italic | alignright aligncenter alignleft | ' +
            'bullist numlist | blockquote link image table | removeformat code fullscreen',
        block_formats: 'متن عادی=p; تیتر ۲=h2; تیتر ۳=h3; تیتر ۴=h4; نقل قول=blockquote',
        content_style:
            "@import url('https://fonts.googleapis.com/css2?family=Vazirmatn:wght@400;700&display=swap');" +
            "body{font-family:'Vazirmatn',Tahoma,sans-serif;font-size:15px;line-height:2.1;direction:rtl}" +
            "img{max-width:100%;height:auto}" +
            "blockquote{border-inline-start:3px solid #C8A24A;padding-inline-start:14px;color:#555}",

        /* تیتر h1 عمداً در فهرست نیست — هر صفحه باید فقط یک h1 داشته باشد
           که همان عنوان مقاله است. اگر نویسنده h1 دوم بگذارد، ساختار سئو خراب می‌شود. */

        setup: function (editor) {
            editor.on('change keyup', function () { editor.save(); });
        }
    });
})();