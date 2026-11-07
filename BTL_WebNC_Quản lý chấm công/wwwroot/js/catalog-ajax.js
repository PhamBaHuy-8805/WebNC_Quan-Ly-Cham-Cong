// Dùng chung cho phòng ban và chức vụ - XHR (list), fetch (search), jQuery (create/delete).
(() => {
    'use strict';
    const root = document.getElementById('catalogAjax');
    if (!root) return;
    const api = root.dataset.api.replace(/\/$/, '');
    const category = root.dataset.name;
    const el = id => document.getElementById(id);
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    let mode = 'list';
    let page = 1;
    let requestNumber = 0;

    function notice(message, isError = false) {
        const box = el('catalogNotice');
        box.textContent = message || '';
        box.className = isError ? 'alert alert-danger mb-2' : (message ? 'alert alert-success mb-2' : 'mb-2');
    }
    function cell(tr, text) {
        const td = document.createElement('td');
        td.textContent = text == null ? '' : String(text);
        tr.appendChild(td);
        return td;
    }
    function navButton(text, number, disabled) {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-outline-primary btn-sm';
        btn.textContent = text;
        btn.disabled = disabled;
        btn.addEventListener('click', () => loadPage(number));
        return btn;
    }
    function render(data) {
        const body = el('catalogRows');
        body.replaceChildren();
        if (!data.items?.length) {
            const tr = document.createElement('tr');
            cell(tr, `Không tìm thấy ${category.toLowerCase()} phù hợp.`).colSpan = 5;
            body.appendChild(tr);
        }
        for (const row of (data.items || [])) {
            const tr = document.createElement('tr');
            cell(tr, row.id);
            cell(tr, row.name);
            cell(tr, row.description);
            cell(tr, row.status === 'Active' ? 'Hoạt động' : 'Ngừng hoạt động');
            const action = cell(tr, '');
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'btn btn-sm btn-outline-danger';
            btn.textContent = 'Xóa';
            btn.addEventListener('click', () => deleteRecord(row.id, row.name));
            action.appendChild(btn);
            body.appendChild(tr);
        }
        page = data.page;
        el('catalogCount').textContent = `Tổng số: ${data.totalItems}`;
        const pager = el('catalogPager');
        pager.replaceChildren();
        pager.appendChild(navButton('« Trước', page - 1, page <= 1));
        const txt = document.createElement('span');
        txt.textContent = `Trang ${page}/${data.totalPages}`;
        pager.appendChild(txt);
        pager.appendChild(navButton('Sau »', page + 1, page >= data.totalPages));
    }

    // 1 + 3. XMLHttpRequest lấy danh sách, phân trang dùng AJAX.
    function loadListXHR(number) {
        const current = ++requestNumber;
        const xhr = new XMLHttpRequest();
        xhr.open('GET', `${api}/list?page=${encodeURIComponent(number)}&pageSize=10`, true);
        xhr.onload = () => {
            if (current !== requestNumber) return;
            try {
                if (xhr.status < 200 || xhr.status >= 300) throw Error('HTTP ' + xhr.status);
                render(JSON.parse(xhr.responseText));
            } catch (e) { notice('Không tải được dữ liệu: ' + e.message, true); }
        };
        xhr.onerror = () => { if (current === requestNumber) notice('Lỗi kết nối.', true); };
        xhr.send();
    }

    // 4. Search bằng fetch() và cập nhật DOM.
    async function loadSearchFetch(number) {
        const current = ++requestNumber;
        const query = new URLSearchParams({ keyword: el('catalogKeyword').value.trim(),
            page: String(number), pageSize: '10' });
        try {
            const response = await fetch(`${api}/search?${query}`);
            if (!response.ok) throw Error('HTTP ' + response.status);
            const result = await response.json();
            if (current === requestNumber) render(result);
        } catch (e) { if (current === requestNumber) notice('Lỗi tìm kiếm: ' + e.message, true); }
    }
    function loadPage(number) {
        if (number < 1) return;
        if (mode === 'search') loadSearchFetch(number);
        else loadListXHR(number);
    }

    // 2. Create/Delete bằng jQuery AJAX + anti-forgery.
    el('catalogCreateForm').addEventListener('submit', event => {
        event.preventDefault();
        const form = event.currentTarget;
        if (!form.reportValidity()) return;
        const submit = form.querySelector('button[type="submit"]');
        submit.disabled = true;
        $.ajax({
            url: `${api}/create`, type: 'POST', data: $(form).serialize(),
            headers: { RequestVerificationToken: token },
            success: result => {
                notice(result.message || 'Đã thêm dữ liệu.');
                form.reset(); mode = 'list'; loadListXHR(1);
            },
            error: xhr => notice(xhr.responseJSON?.message || 'Thêm thất bại.', true),
            complete: () => { submit.disabled = false; }
        });
    });
    function deleteRecord(id, name) {
        if (!confirm(`Xóa ${category.toLowerCase()} "${name}"?`)) return;
        $.ajax({
            url: `${api}/delete/${encodeURIComponent(id)}`, type: 'POST',
            headers: { RequestVerificationToken: token },
            success: result => { notice(result.message || 'Đã xóa.'); loadPage(page); },
            error: xhr => notice(xhr.responseJSON?.message || 'Xóa thất bại.', true)
        });
    }
    el('catalogSearch').addEventListener('click', () => { mode = 'search'; notice(''); loadSearchFetch(1); });
    el('catalogReset').addEventListener('click', () => {
        el('catalogKeyword').value = ''; mode = 'list'; notice(''); loadListXHR(1);
    });
    el('catalogKeyword').addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); mode = 'search'; loadSearchFetch(1); }
    });
    loadListXHR(1);
})();
