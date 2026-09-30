/* Core helpers shared by every page: auth, API calls, layout (sidebar), formatting, modals, toasts. */
'use strict';

const App = (() => {
    const KEY = { token: 'diag.token', user: 'diag.user', menus: 'diag.menus', module: 'diag.module' };

    const store = {
        get(k) { try { return JSON.parse(localStorage.getItem(k)); } catch { return null; } },
        set(k, v) { try { localStorage.setItem(k, JSON.stringify(v)); } catch { /* storage blocked */ } },
        del(k) { try { localStorage.removeItem(k); } catch { /* storage blocked */ } },
    };

    const user = () => store.get(KEY.user);
    const token = () => user()?.token;

    function logout() {
        Object.values(KEY).forEach(store.del);
        location.href = '/Home/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search);
    }

    // ---------------------------------------------------------------- HTTP
    async function request(base, path, { method = 'GET', body, blob = false } = {}) {
        const headers = {};
        if (token()) headers.Authorization = 'Bearer ' + token();
        if (body !== undefined) headers['Content-Type'] = 'application/json';
        let res;
        try {
            res = await fetch(base + path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
        } catch {
            throw new Error('Server is not reachable (' + base + ').');
        }
        if (res.status === 401 && !path.includes('/auth/login')) { logout(); throw new Error('Session expired.'); }
        if (!res.ok) {
            let msg = res.status === 403 ? 'You do not have permission for this action.' : 'Request failed (' + res.status + ').';
            try {
                const j = await res.json();
                if (j.message) msg = j.message;
                else if (j.errors) msg = Object.values(j.errors).flat().join(' ');
                else if (j.title) msg = j.title;
            } catch { /* not json */ }
            throw new Error(msg);
        }
        if (blob) return res.blob();
        if (res.status === 204) return null;
        const text = await res.text();
        return text ? JSON.parse(text) : null;
    }

    const api = (path, opt) => request(APP.apiBase, path, opt);

    /** Opens a PDF from the report server in a new tab (or downloads a CSV). */
    async function report(path, { download } = {}) {
        const win = download ? null : window.open('', '_blank');
        if (win) win.document.write('<p style="font-family:sans-serif;padding:2rem">Generating report…</p>');
        try {
            const b = await request(APP.reportBase, path, { blob: true });
            const url = URL.createObjectURL(b);
            if (download) {
                const a = document.createElement('a');
                a.href = url; a.download = download; a.click();
            } else if (win) win.location.href = url;
            else window.open(url, '_blank');
        } catch (e) {
            if (win) win.close();
            toast(e.message, 'danger');
        }
    }

    // ---------------------------------------------------------------- UI helpers
    function toast(message, type = 'success') {
        const el = document.createElement('div');
        el.className = `toast align-items-center text-bg-${type} border-0`;
        el.innerHTML = `<div class="d-flex"><div class="toast-body">${esc(message)}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>`;
        document.getElementById('toasts')?.appendChild(el);
        const t = new bootstrap.Toast(el, { delay: type === 'danger' ? 6000 : 3000 });
        el.addEventListener('hidden.bs.toast', () => el.remove());
        t.show();
    }

    /** Runs an async action, shows its error as a toast and disables the clicked button meanwhile. */
    async function run(fn, button) {
        const old = button?.innerHTML;
        if (button) { button.disabled = true; button.innerHTML = '<span class="spinner-border spinner-border-sm"></span>'; }
        try { return await fn(); }
        catch (e) { toast(e.message, 'danger'); }
        finally { if (button) { button.disabled = false; button.innerHTML = old; } }
    }

    function modal(title, bodyHtml, { size = '', footer = '', static: isStatic = false } = {}) {
        const el = document.createElement('div');
        el.className = 'modal fade';
        el.tabIndex = -1;
        el.innerHTML = `<div class="modal-dialog modal-dialog-scrollable ${size}"><div class="modal-content">
            <div class="modal-header py-2"><h6 class="modal-title fw-bold">${title}</h6>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
            <div class="modal-body">${bodyHtml}</div>
            ${footer ? `<div class="modal-footer py-2">${footer}</div>` : ''}</div></div>`;
        document.body.appendChild(el);
        const m = new bootstrap.Modal(el, isStatic ? { backdrop: 'static' } : {});
        el.addEventListener('hidden.bs.modal', () => el.remove());
        m.show();
        return { el, close: () => m.hide(), q: s => el.querySelector(s), qa: s => el.querySelectorAll(s) };
    }

    function confirmBox(message, okText = 'Yes') {
        return new Promise(resolve => {
            let result = false;
            const m = modal('Please confirm', `<p class="mb-0">${esc(message)}</p>`, {
                size: 'modal-sm',
                footer: `<button class="btn btn-light" data-bs-dismiss="modal">Cancel</button><button class="btn btn-primary" data-ok>${okText}</button>`,
            });
            m.q('[data-ok]').onclick = () => { result = true; m.close(); };
            m.el.addEventListener('hidden.bs.modal', () => resolve(result));
        });
    }

    function promptBox(title, label, { value = '', required = true, textarea = false } = {}) {
        return new Promise(resolve => {
            let result = null;
            const input = textarea
                ? `<textarea class="form-control" rows="3" data-in>${esc(value)}</textarea>`
                : `<input class="form-control" data-in value="${esc(value)}">`;
            const m = modal(title, `<label class="form-label ${required ? 'required' : ''}">${esc(label)}</label>${input}`, {
                footer: `<button class="btn btn-light" data-bs-dismiss="modal">Cancel</button><button class="btn btn-primary" data-ok>OK</button>`,
            });
            const inp = m.q('[data-in]');
            setTimeout(() => inp.focus(), 300);
            m.q('[data-ok]').onclick = () => {
                if (required && !inp.value.trim()) { inp.classList.add('is-invalid'); return; }
                result = inp.value.trim(); m.close();
            };
            m.el.addEventListener('hidden.bs.modal', () => resolve(result));
        });
    }

    // ---------------------------------------------------------------- formatting
    function esc(s) {
        return String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }
    const money = v => Number(v ?? 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    const pad = n => String(n).padStart(2, '0');
    const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    function date(v) {
        if (!v) return '';
        const d = new Date(v);
        return isNaN(d) ? v : `${pad(d.getDate())}-${MONTHS[d.getMonth()]}-${d.getFullYear()}`;
    }
    function dateTime(v) {
        if (!v) return '';
        const d = new Date(v);
        if (isNaN(d)) return v;
        let h = d.getHours(); const ap = h >= 12 ? 'PM' : 'AM'; h = h % 12 || 12;
        return `${date(v)} ${pad(h)}:${pad(d.getMinutes())} ${ap}`;
    }
    const isoDate = (d = new Date()) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
    const status = s => `<span class="status st-${esc(s)}">${esc(String(s ?? '').replace(/([a-z])([A-Z])/g, '$1 $2'))}</span>`;
    const debounce = (fn, ms = 300) => { let t; return (...a) => { clearTimeout(t); t = setTimeout(() => fn(...a), ms); }; };
    const qs = obj => {
        const p = new URLSearchParams();
        Object.entries(obj).forEach(([k, v]) => { if (v !== undefined && v !== null && v !== '') p.append(k, v); });
        const s = p.toString();
        return s ? '?' + s : '';
    };

    const lookupCache = {};
    function lookup(path) {
        lookupCache[path] ??= api(path).catch(e => { delete lookupCache[path]; throw e; });
        return lookupCache[path];
    }
    const options = (items, selected, placeholder = '-- Select --') =>
        (placeholder !== null ? `<option value="">${placeholder}</option>` : '') +
        items.map(i => `<option value="${i.id}" ${String(i.id) === String(selected ?? '') ? 'selected' : ''}>${esc(i.name)}</option>`).join('');

    // ---------------------------------------------------------------- layout & permissions
    let menusPromise = null;
    /** Menus + rights of the user, fetched once per page load so new menus / permission changes show at once.
     *  The last copy is kept only as a fallback when the API cannot be reached. */
    function menus() {
        menusPromise ??= api('/api/auth/menus').then(projects => {
            store.set(KEY.menus, { userId: user()?.userId, projects });
            return projects;
        }).catch(e => {
            const cached = store.get(KEY.menus);
            if (cached && cached.userId === user()?.userId) return cached.projects;
            menusPromise = null;
            throw e;
        });
        return menusPromise;
    }

    const samePath = (a, b) => (a || '').toLowerCase().replace(/\/$/, '') === (b || '').toLowerCase().replace(/\/$/, '');

    /** Rights of the logged-in user on the current page (from Menu Permission). */
    async function rights(path = location.pathname) {
        const projects = await menus();
        for (const p of projects) {
            const m = p.menus.find(x => samePath(x.url, path));
            if (m) return m;
        }
        return user()?.isSuperAdmin ? { canView: true, canAdd: true, canEdit: true, canDelete: true, canPrint: true } : {};
    }

    function setModule(code) { store.set(KEY.module, code); }

    async function renderShell() {
        const u = user();
        if (!u) { logout(); return; }
        document.getElementById('userName').textContent = u.fullName;
        document.getElementById('userRole').textContent = u.roles?.join(', ') || '';
        document.getElementById('userAvatar').textContent = u.fullName.split(' ').map(s => s[0]).slice(0, 2).join('').toUpperCase();
        document.getElementById('tenantTitle').textContent = u.tenantName ? `${u.tenantName}` : APP.appName;

        const shell = document.getElementById('appShell');
        document.getElementById('toggleSidebar').onclick = () =>
            shell.classList.toggle(window.innerWidth < 992 ? 'open' : 'collapsed');
        document.getElementById('btnLogout').onclick = e => { e.preventDefault(); logout(); };
        document.getElementById('btnChangePassword').onclick = e => { e.preventDefault(); changePassword(); };

        let projects;
        try {
            projects = await menus();
            document.getElementById('connStatus').textContent = 'Connected';
        } catch (e) {
            document.getElementById('connStatus').textContent = 'Offline – ' + e.message;
            return;
        }

        let current = projects.find(p => p.menus.some(m => samePath(m.url, location.pathname)));
        if (current) setModule(current.code);
        else current = projects.find(p => p.code === store.get(KEY.module)) ?? null;

        document.getElementById('moduleList').innerHTML = projects.map(p =>
            `<li><a class="dropdown-item" href="/Home/Module/${esc(p.code)}"><i class="bi ${esc(p.icon || 'bi-grid')} me-2"></i>${esc(p.name)}</a></li>`).join('')
            + '<li><hr class="dropdown-divider"></li><li><a class="dropdown-item" href="/"><i class="bi bi-house me-2"></i>All modules</a></li>';

        const nav = document.getElementById('sideMenu');
        if (!current) {
            nav.innerHTML = projects.map(p => `<a href="/Home/Module/${esc(p.code)}"><span class="menu-icon"><i class="bi ${esc(p.icon || 'bi-grid')}"></i></span>${esc(p.name)}</a>`).join('');
            return;
        }
        document.getElementById('moduleName').textContent = current.name.toUpperCase();
        document.getElementById('moduleIcon').className = 'bi ' + (current.icon || 'bi-grid');
        nav.innerHTML = current.menus.filter(m => m.url).map(m =>
            `<a href="${esc(m.url)}" class="${samePath(m.url, location.pathname) ? 'active' : ''}">
                <span class="menu-icon"><i class="bi ${esc(m.icon || 'bi-chevron-right')}"></i></span>${esc(m.name)}</a>`).join('');
        nav.querySelector('a.active')?.scrollIntoView({ block: 'center' });
    }

    function changePassword() {
        const m = modal('Change password', `
            <div class="mb-2"><label class="form-label required">Current password</label><input type="password" class="form-control" data-cur></div>
            <div class="mb-2"><label class="form-label required">New password</label><input type="password" class="form-control" data-new></div>
            <div><label class="form-label required">Confirm new password</label><input type="password" class="form-control" data-conf></div>`,
            { size: 'modal-sm', footer: '<button class="btn btn-primary" data-ok>Change</button>' });
        m.q('[data-ok]').onclick = e => run(async () => {
            const cur = m.q('[data-cur]').value, nw = m.q('[data-new]').value;
            if (nw !== m.q('[data-conf]').value) throw new Error('New passwords do not match.');
            await api('/api/auth/change-password', { method: 'POST', body: { currentPassword: cur, newPassword: nw } });
            m.close();
            toast('Password changed.');
        }, e.currentTarget);
    }

    function ready(fn) {
        if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', fn);
        else fn();
    }

    ready(() => { if (!window.APP.noShell) renderShell(); });

    return {
        store, user, token, logout, api, report, toast, run, modal, confirmBox, promptBox,
        esc, money, date, dateTime, isoDate, status, debounce, qs, lookup, options, menus, rights, setModule, ready, KEY,
    };
})();
