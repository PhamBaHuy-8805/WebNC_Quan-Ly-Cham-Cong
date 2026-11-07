(() => {
    'use strict';
    const sidebar = document.getElementById('app-sidebar');
    const toggle = document.getElementById('menu-toggle');
    const backdrop = document.getElementById('sidebar-backdrop');
    const mobile = window.matchMedia('(max-width: 991px)');
    function setMenu(open, returnFocus = false) {
        if (!sidebar || !toggle || !backdrop) return;
        sidebar.classList.toggle('is-open', open);
        document.body.classList.toggle('menu-open', open);
        toggle.setAttribute('aria-expanded', String(open));
        toggle.setAttribute('aria-label', open ? 'Đóng menu' : 'Mở menu');
        backdrop.hidden = !open;
        if (mobile.matches) sidebar.inert = !open;
        if (open) sidebar.querySelector('a')?.focus();
        else if (returnFocus) toggle.focus();
    }
    if (sidebar && toggle && backdrop) {
        sidebar.inert = mobile.matches;
        toggle.addEventListener('click', () => setMenu(!sidebar.classList.contains('is-open')));
        backdrop.addEventListener('click', () => setMenu(false, true));
        mobile.addEventListener('change', () => { setMenu(false); sidebar.inert = mobile.matches; });
        document.addEventListener('keydown', event => {
            if (!sidebar.classList.contains('is-open')) return;
            if (event.key === 'Escape') setMenu(false, true);
            if (event.key === 'Tab') {
                const focusable = [...sidebar.querySelectorAll('a, button')].filter(el => el.offsetParent !== null);
                const first = focusable[0]; const last = focusable[focusable.length - 1];
                if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
                else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
            }
        });
        const normalize = value => value.toLowerCase().replace(/\/$/, '') || '/';
        let current = normalize(location.pathname);
        const aliases = [['/employee', '/employees'], ['/employeeajax', '/employees'], ['/employeeedit', '/employees'], ['/ajax/employees', '/employees'], ['/department', '/departments'], ['/ajax/departments', '/departments'], ['/position', '/positions'], ['/ajax/positions', '/positions']];
        for (const [prefix, target] of aliases) if (current === prefix || current.startsWith(prefix + '/')) { current = target; break; }
        let best = null; let length = -1;
        sidebar.querySelectorAll('.side-link').forEach(link => {
            const path = normalize(new URL(link.href).pathname);
            if ((current === path || (path !== '/' && current.startsWith(path + '/'))) && path.length > length) { best = link; length = path.length; }
        });
        if (!best && current === '/home') best = sidebar.querySelector('.side-link');
        if (best) { best.classList.add('is-active'); best.setAttribute('aria-current', 'page'); }
    }
    const password = document.getElementById('Password');
    const passwordToggle = document.getElementById('password-toggle');
    passwordToggle?.addEventListener('click', () => {
        if (!password) return;
        const show = password.type === 'password';
        password.type = show ? 'text' : 'password';
        passwordToggle.textContent = show ? 'Ẩn' : 'Hiện';
        passwordToggle.setAttribute('aria-pressed', String(show));
        passwordToggle.setAttribute('aria-label', show ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
    });
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
    if (reducedMotion.matches || !('IntersectionObserver' in window)) return;
    document.body.classList.add('motion-ready');
    const observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
            if (entry.isIntersecting) { entry.target.classList.add('is-visible'); observer.unobserve(entry.target); }
        });
    }, { threshold: 0.03 });
    function reveal(root) {
        if (!(root instanceof Element)) return;
        const targets = root.matches('.card, .table-responsive, .feature-card, .home-hero, .login-layout') ? [root] : [];
        targets.push(...root.querySelectorAll('.card, .table-responsive, .feature-card, .home-hero, .login-layout'));
        targets.forEach(element => {
            if (element.hasAttribute('data-reveal')) return;
            element.setAttribute('data-reveal', '');
            observer.observe(element);
        });
    }
    const main = document.getElementById('main-content');
    if (main) {
        reveal(main);
        new MutationObserver(records => records.forEach(record => record.addedNodes.forEach(reveal)))
            .observe(main, { childList: true, subtree: true });
    }
    reducedMotion.addEventListener('change', event => {
        if (event.matches) { document.body.classList.remove('motion-ready'); observer.disconnect(); }
    });
})();
