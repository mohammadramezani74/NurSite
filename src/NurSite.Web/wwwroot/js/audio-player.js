/* پخش‌کننده صوت — نوار موج، پرش پانزده‌ثانیه‌ای، سرعت پخش و شمارش پخش.

   بهسازی تدریجی: تگ <audio> با کنترل‌های خود مرورگر در HTML هست و اگر
   این فایل اجرا نشود کاربر باز هم می‌تواند گوش بدهد. اینجا آن کنترل‌ها
   برداشته و رابط خودمان جایش ساخته می‌شود. */
(function () {
    'use strict';

    var BAR_COUNT = 64;
    var SKIP = 15;                 // ثانیه پرش جلو و عقب
    var COUNT_AFTER = 15;          // بعد از این مقدار شنیدن، پخش شمرده می‌شود
    var SPEEDS = [0.75, 1, 1.25, 1.5, 2];
    var VOLUME_KEY = 'nur:volume';

    /* بلندی صدای انتخابی کاربر بین صفحه‌ها می‌ماند. کسی که یک مجموعه
       ده جلسه‌ای گوش می‌دهد نباید هر بار دوباره تنظیمش کند. */
    function savedVolume() {
        try {
            var value = parseFloat(window.localStorage.getItem(VOLUME_KEY));
            return isFinite(value) && value >= 0 && value <= 1 ? value : 1;
        } catch (e) {
            return 1;
        }
    }

    function rememberVolume(value) {
        try { window.localStorage.setItem(VOLUME_KEY, String(value)); } catch (e) { /* حالت ناشناس مرورگر */ }
    }

    /* آیفون بلندی صدا را فقط با دکمه‌های خود دستگاه می‌پذیرد و مقداردهی
       از راه جاوااسکریپت را بی‌صدا نادیده می‌گیرد. لغزنده‌ای که کار نکند
       بدتر از نبودنش است، پس اول امتحان می‌کنیم. */
    function volumeWorks(audio) {
        var original = audio.volume;
        try {
            audio.volume = 0.31;
            var works = Math.abs(audio.volume - 0.31) < 0.01;
            audio.volume = original;
            return works;
        } catch (e) {
            return false;
        }
    }

    function faDigits(value) {
        return String(value).replace(/[0-9]/g, function (d) {
            return String.fromCharCode(d.charCodeAt(0) + 0x06F0 - 48);
        });
    }

    function clock(seconds) {
        if (!isFinite(seconds) || seconds < 0) seconds = 0;
        var s = Math.floor(seconds % 60);
        var m = Math.floor(seconds / 60) % 60;
        var h = Math.floor(seconds / 3600);
        var text = h > 0
            ? h + ':' + String(m).padStart(2, '0') + ':' + String(s).padStart(2, '0')
            : m + ':' + String(s).padStart(2, '0');
        return faDigits(text);
    }

    /* ---------- شکل موج ----------
       موج واقعی یعنی خواندن دامنه صوت، که تا کل فایل دانلود نشود ممکن
       نیست و برای یک سخنرانی چهل مگابایتی منطقی نیست. پس شکل موج از روی
       اسلاگ ساخته می‌شود: تزئینی است ولی برای هر اثر همیشه یکسان می‌ماند،
       نه اینکه با هر بار بازکردن صفحه فرق کند. */
    function seedFrom(text) {
        var hash = 2166136261;
        for (var i = 0; i < text.length; i++) {
            hash ^= text.charCodeAt(i);
            hash = Math.imul(hash, 16777619);
        }
        return hash >>> 0;
    }

    function randomizer(seed) {
        return function () {
            seed |= 0; seed = seed + 0x6D2B79F5 | 0;
            var t = Math.imul(seed ^ seed >>> 15, 1 | seed);
            t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
            return ((t ^ t >>> 14) >>> 0) / 4294967296;
        };
    }

    function buildBars(container, seed) {
        var next = randomizer(seed);
        var previous = 0.5;

        for (var i = 0; i < BAR_COUNT; i++) {
            /* هر میله کمی به میله قبلی نزدیک می‌ماند تا نتیجه مثل صدا
               موج بخورد، نه مثل نویز تصادفی */
            var target = next();
            previous = previous * 0.55 + target * 0.45;

            /* کمی برجستگی در میانه، شبیه اوج گرفتن یک سخنرانی */
            var middle = 1 - Math.abs(i / BAR_COUNT - 0.5) * 0.8;
            var height = Math.max(0.12, Math.min(1, previous * middle * 1.3));

            var bar = document.createElement('span');
            bar.className = 'p-bar';
            bar.style.height = (height * 100).toFixed(1) + '%';
            container.appendChild(bar);
        }
    }

    /* ---------- ساخت رابط ---------- */
    function icon(paths, filled) {
        return '<svg viewBox="0 0 24 24" fill="' + (filled ? 'currentColor' : 'none') +
            '" stroke="currentColor" stroke-width="1.8" aria-hidden="true">' + paths + '</svg>';
    }

    var ICON_PLAY = icon('<path d="M8 5.5v13l11-6.5Z"/>', true);
    var ICON_PAUSE = icon('<path d="M9 5v14M15 5v14"/>', false);
    var ICON_BACK = icon('<path d="M11 5 4 12l7 7"/><path d="M4 12h9a7 7 0 1 1 0 14h-3"/>', false);
    var ICON_FWD = icon('<path d="m13 5 7 7-7 7"/><path d="M20 12h-9a7 7 0 1 0 0 14h3"/>', false);
    var ICON_VOL = icon('<path d="M11 5 6 9H3v6h3l5 4Z"/><path d="M16 9a4 4 0 0 1 0 6"/>', false);
    var ICON_MUTE = icon('<path d="M11 5 6 9H3v6h3l5 4Z"/><path d="m16 10 4 4M20 10l-4 4"/>', false);

    function setup(root) {
        var audio = root.querySelector('audio');
        if (!audio) return;

        var id = root.dataset.id;
        var seed = seedFrom(root.dataset.seed || 'nur');
        var known = parseInt(root.dataset.duration, 10) || 0;

        audio.removeAttribute('controls');

        var shell = document.createElement('div');
        shell.className = 'p-shell';
        shell.innerHTML =
            '<button type="button" class="p-play" aria-label="پخش">' + ICON_PLAY + '</button>' +
            '<div class="p-mid">' +
            '  <div class="p-wave" role="slider" tabindex="0" aria-label="جای پخش"' +
            '       aria-valuemin="0" aria-valuemax="100" aria-valuenow="0"></div>' +
            '  <div class="p-times"><span class="p-now">۰:۰۰</span><span class="p-total">' +
            (known ? clock(known) : '۰:۰۰') + '</span></div>' +
            '</div>' +
            '<div class="p-tools">' +
            '  <button type="button" class="p-skip" data-by="-' + SKIP + '" aria-label="۱۵ ثانیه عقب">' + ICON_BACK + '</button>' +
            '  <button type="button" class="p-skip" data-by="' + SKIP + '" aria-label="۱۵ ثانیه جلو">' + ICON_FWD + '</button>' +
            '  <button type="button" class="p-rate" aria-label="سرعت پخش">۱×</button>' +
            '  <div class="p-volume">' +
            '    <button type="button" class="p-mute" aria-label="بی‌صدا">' + ICON_VOL + '</button>' +
            '    <input type="range" class="p-vol" min="0" max="1" step="0.05"' +
            '           aria-label="بلندی صدا">' +
            '  </div>' +
            '</div>';

        root.appendChild(shell);
        root.classList.add('ready');

        var wave = shell.querySelector('.p-wave');
        var playBtn = shell.querySelector('.p-play');
        var nowLabel = shell.querySelector('.p-now');
        var totalLabel = shell.querySelector('.p-total');
        var rateBtn = shell.querySelector('.p-rate');
        var muteBtn = shell.querySelector('.p-mute');
        var volumeBox = shell.querySelector('.p-volume');
        var volumeInput = shell.querySelector('.p-vol');

        buildBars(wave, seed);
        var bars = wave.querySelectorAll('.p-bar');

        /* ---------- پخش و توقف ---------- */
        function toggle() {
            if (audio.paused) { audio.play(); } else { audio.pause(); }
        }

        playBtn.addEventListener('click', toggle);

        audio.addEventListener('play', function () {
            playBtn.innerHTML = ICON_PAUSE;
            playBtn.setAttribute('aria-label', 'توقف');
            root.classList.add('playing');
        });

        audio.addEventListener('pause', function () {
            playBtn.innerHTML = ICON_PLAY;
            playBtn.setAttribute('aria-label', 'پخش');
            root.classList.remove('playing');
        });

        /* ---------- پیشرفت ---------- */
        function duration() {
            return isFinite(audio.duration) && audio.duration > 0 ? audio.duration : known;
        }

        function paint() {
            var total = duration();
            var ratio = total > 0 ? audio.currentTime / total : 0;
            var filled = Math.round(ratio * BAR_COUNT);

            for (var i = 0; i < bars.length; i++) {
                bars[i].classList.toggle('on', i < filled);
                bars[i].classList.toggle('head', i === filled - 1);
            }

            nowLabel.textContent = clock(audio.currentTime);
            wave.setAttribute('aria-valuenow', Math.round(ratio * 100));
            wave.setAttribute('aria-valuetext', clock(audio.currentTime) + ' از ' + clock(total));
        }

        audio.addEventListener('loadedmetadata', function () {
            totalLabel.textContent = clock(duration());
            paint();
        });

        audio.addEventListener('timeupdate', paint);
        audio.addEventListener('ended', function () { paint(); });

        /* ---------- جابه‌جایی روی نوار ----------
           نوار عمداً چپ‌به‌راست است. صفحه راست‌به‌چپ است ولی خط زمان در
           همه پخش‌کننده‌ها از چپ شروع می‌شود و برعکسش گیج‌کننده است. */
        function seekTo(clientX) {
            var box = wave.getBoundingClientRect();
            var ratio = (clientX - box.left) / box.width;
            ratio = Math.max(0, Math.min(1, ratio));

            var total = duration();
            if (total > 0) {
                audio.currentTime = ratio * total;
                paint();
            }
        }

        wave.addEventListener('pointerdown', function (e) {
            seekTo(e.clientX);
            wave.setPointerCapture(e.pointerId);

            function move(ev) { seekTo(ev.clientX); }
            function up(ev) {
                wave.releasePointerCapture(ev.pointerId);
                wave.removeEventListener('pointermove', move);
                wave.removeEventListener('pointerup', up);
            }

            wave.addEventListener('pointermove', move);
            wave.addEventListener('pointerup', up);
        });

        wave.addEventListener('keydown', function (e) {
            var step = e.key === 'ArrowLeft' ? -5 : e.key === 'ArrowRight' ? 5 : 0;
            if (step === 0) {
                if (e.key === ' ' || e.key === 'Enter') { e.preventDefault(); toggle(); }
                return;
            }
            e.preventDefault();
            audio.currentTime = Math.max(0, Math.min(duration(), audio.currentTime + step));
            paint();
        });

        /* ---------- پرش، سرعت، بی‌صدا ---------- */
        shell.querySelectorAll('.p-skip').forEach(function (button) {
            button.addEventListener('click', function () {
                var by = parseInt(button.dataset.by, 10);
                audio.currentTime = Math.max(0, Math.min(duration(), audio.currentTime + by));
                paint();
            });
        });

        var rateIndex = 1;
        rateBtn.addEventListener('click', function () {
            rateIndex = (rateIndex + 1) % SPEEDS.length;
            audio.playbackRate = SPEEDS[rateIndex];
            rateBtn.textContent = faDigits(String(SPEEDS[rateIndex])) + '×';
        });

        /* ---------- بلندی صدا ---------- */
        if (!volumeWorks(audio)) {
            /* روی این دستگاه فقط دکمه‌های خود گوشی صدا را کم و زیاد می‌کنند */
            volumeBox.remove();
        } else {
            var lastAudible = savedVolume() || 1;
            audio.volume = savedVolume();
            volumeInput.value = audio.volume;

            function paintVolume() {
                var quiet = audio.muted || audio.volume === 0;
                muteBtn.innerHTML = quiet ? ICON_MUTE : ICON_VOL;
                muteBtn.setAttribute('aria-label', quiet ? 'باصدا' : 'بی‌صدا');
                volumeInput.value = quiet ? 0 : audio.volume;
                /* رنگ پرشدهٔ لغزنده تا همان‌جایی که صدا هست */
                volumeInput.style.setProperty('--fill', (volumeInput.value * 100) + '%');
            }

            volumeInput.addEventListener('input', function () {
                var value = parseFloat(volumeInput.value);
                audio.volume = value;
                audio.muted = value === 0;
                if (value > 0) lastAudible = value;

                rememberVolume(value);
                paintVolume();
            });

            muteBtn.addEventListener('click', function () {
                if (audio.muted || audio.volume === 0) {
                    /* برگشت به همان بلندی قبلی، نه به بیشینه */
                    audio.muted = false;
                    audio.volume = lastAudible;
                } else {
                    lastAudible = audio.volume;
                    audio.muted = true;
                }

                rememberVolume(audio.muted ? 0 : audio.volume);
                paintVolume();
            });

            paintVolume();
        }

        /* ---------- شمارش پخش ----------
           فقط وقتی که واقعاً پانزده ثانیه شنیده شده باشد، و یک بار در
           هر بار باز کردن صفحه. */
        if (id) {
            var listened = 0;
            var last = 0;
            var counted = false;

            audio.addEventListener('timeupdate', function () {
                if (counted) return;

                var delta = audio.currentTime - last;
                last = audio.currentTime;

                /* پرش کاربر روی نوار نباید به حساب شنیدن گذاشته شود */
                if (delta > 0 && delta < 2) listened += delta;
                if (listened < COUNT_AFTER) return;

                counted = true;
                fetch('/api/pakhsh/' + id, { method: 'POST', keepalive: true })
                    .catch(function () { /* شمارش پخش آنقدر مهم نیست که خطایش را به کاربر نشان دهیم */ });
            });
        }

        paint();
    }

    document.querySelectorAll('[data-player]').forEach(setup);
})();