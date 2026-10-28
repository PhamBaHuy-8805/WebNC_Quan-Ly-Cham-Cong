// Bài thực hành AJAX - Employee: XMLHttpRequest, jQuery AJAX, fetch, Skip/Take.
(() => {
    'use strict';
    const root = document.getElementById('employeeAjax');
    if (!root) return;
    const api = root.dataset.baseUrl.replace(/\/$/, '');
    const $id = id => document.getElementById(id);
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const money = new Intl.NumberFormat('vi-VN');
    let mode = 'list'; // 'list' -> XHR, 'search' -> fetch
    let currentPage = 1;
    let requestNumber = 0;

    function notice(message, isError = false) {
        const el = $id('empNotice');
        el.textContent = message || '';
        el.className = isError ? 'alert alert-danger mb-2' : (message ? 'alert alert-success mb-2' : 'mb-2');
    }
    function makeCell(tr, value) {
        const td = document.createElement('td');
        td.textContent = value == null ? '' : String(value);
        tr.appendChild(td);
        return td;
    }
    function makeButton(label, page, disabled = false) {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-outline-primary btn-sm';
        btn.textContent = label;
        btn.disabled = disabled;
        btn.addEventListener('click', () => loadPage(page));
        return btn;
    }
    function render(result) {
        const body = $id('empRows');
        body.replaceChildren();
        const items = result.items || [];
        if (items.length === 0) {
            const tr = document.createElement('tr');
            const td = makeCell(tr, 'Không tìm thấy nhân viên phù hợp.');
            td.colSpan = 10;
            body.appendChild(tr);
        }
        for (const e of items) {
            const tr = document.createElement('tr');
            makeCell(tr, e.employeeID);
            makeCell(tr, e.employeeCode);
            makeCell(tr, e.fullName);
            makeCell(tr, e.departmentName);
            makeCell(tr, e.positionName);
            makeCell(tr, e.email);
            makeCell(tr, (e.hireDate || '').slice(0, 10));
            makeCell(tr, money.format(e.salary ?? 0));
            makeCell(tr, e.status === 'Active' ? 'Đang làm' : 'Ngừng làm');
            const actions = makeCell(tr, '');
            const del = document.createElement('button');
            del.type = 'button';
            del.className = 'btn btn-sm btn-outline-danger';
            del.textContent = 'Xóa';
            del.addEventListener('click', () => deleteEmployee(e.employeeID, e.fullName));
            actions.appendChild(del);
            body.appendChild(tr);
        }
        currentPage = result.page;
        $id('empCount').textContent = `Tổng số: ${result.totalItems} nhân viên`;
        const pager = $id('empPager');
        pager.replaceChildren();
        pager.appendChild(makeButton('« Trước', currentPage - 1, currentPage <= 1));
        const position = document.createElement('span');
        position.textContent = `Trang ${currentPage}/${result.totalPages}`;
        pager.appendChild(position);
        pager.appendChild(makeButton('Sau »', currentPage + 1, currentPage >= result.totalPages));
    }

    // 1. Hiển thị và phân trang bằng XMLHttpRequest (không reload).
    function loadEmployeesXHR(page) {
        const myRequest = ++requestNumber;
        const xhr = new XMLHttpRequest();
        xhr.open('GET', `${api}/list?page=${encodeURIComponent(page)}&pageSize=10`, true);
        xhr.onload = () => {
            if (myRequest !== requestNumber) return;
            try {
                if (xhr.status < 200 || xhr.status >= 300) throw Error('HTTP ' + xhr.status);
                render(JSON.parse(xhr.responseText));
            } catch (e) { notice('Không tải được danh sách nhân viên: ' + e.message, true); }
        };
        xhr.onerror = () => { if (myRequest === requestNumber) notice('Lỗi kết nối khi tải danh sách.', true); };
        xhr.send();
    }

    // 4. AJAX Search bằng fetch() nhận JSON, render DOM.
    async function loadSearchFetch(page) {
        const myRequest = ++requestNumber;
        const p = new URLSearchParams({
            keyword: $id('empKeyword').value.trim(),
            departmentId: $id('empDepartmentFilter').value,
            positionId: $id('empPositionFilter').value,
            status: $id('empStatusFilter').value,
            page: String(page), pageSize: '10'
        });
        try {
            const response = await fetch(`${api}/search?${p.toString()}`);
            if (!response.ok) throw Error('HTTP ' + response.status);
            const result = await response.json();
            if (myRequest === requestNumber) render(result);
        } catch (e) { if (myRequest === requestNumber) notice('Lỗi khi tìm kiếm: ' + e.message, true); }
    }
    function loadPage(page) {
        if (page < 1) return;
        if (mode === 'search') loadSearchFetch(page);
        else loadEmployeesXHR(page);
    }

    // Dropdown có giá trị ID nhưng hiển thị tên phòng ban/chức vụ.
    async function loadLookups() {
        try {
            const response = await fetch(`${api}/lookups`);
            if (!response.ok) throw Error('HTTP ' + response.status);
            const data = await response.json();
            const addOptions = (selectId, items) => {
                const select = $id(selectId);
                // Giữ nguyên option đầu tiên.
                while (select.options.length > 1) select.remove(1);
                items.forEach(x => {
                    const opt = document.createElement('option');
                    opt.value = String(x.id);
                    opt.textContent = x.name;
                    select.appendChild(opt);
                });
            };
            addOptions('empDepartmentFilter', data.departments);
            addOptions('empDepartmentCreate', data.departments);
            addOptions('empPositionFilter', data.positions);
            addOptions('empPositionCreate', data.positions);
        } catch (e) { notice('Không tải được phòng ban/chức vụ: ' + e.message, true); }
    }

    // 2. Tạo nhân viên bằng jQuery AJAX.
    function createEmployee(event) {
        event.preventDefault();
        const form = $id('empCreateForm');
        if (!form.reportValidity()) return;
        const button = $id('empSave');
        button.disabled = true;
        $.ajax({
            url: `${api}/create`, type: 'POST', data: $(form).serialize(),
            headers: { RequestVerificationToken: token },
            success: data => {
                notice(data.message || 'Đã thêm nhân viên.');
                form.reset();
                mode = 'list';
                loadEmployeesXHR(1);
            },
            error: xhr => notice(xhr.responseJSON?.message || 'Không thể thêm nhân viên.', true),
            complete: () => { button.disabled = false; }
        });
    }

    // 2. Xóa nhân viên bằng jQuery AJAX.
    function deleteEmployee(id, name) {
        if (!confirm(`Bạn có chắc muốn xóa nhân viên "${name}"?`)) return;
        $.ajax({
            url: `${api}/delete/${encodeURIComponent(id)}`, type: 'POST',
            headers: { RequestVerificationToken: token },
            success: data => { notice(data.message || 'Đã xóa.'); loadPage(currentPage); },
            error: xhr => notice(xhr.responseJSON?.message || 'Không thể xóa nhân viên.', true)
        });
    }

    $id('empSearch').addEventListener('click', () => { mode = 'search'; notice(''); loadSearchFetch(1); });
    $id('empReset').addEventListener('click', () => {
        $id('empKeyword').value = '';
        $id('empDepartmentFilter').value = '';
        $id('empPositionFilter').value = '';
        $id('empStatusFilter').value = '';
        mode = 'list'; notice(''); loadEmployeesXHR(1);
    });
    $id('empKeyword').addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); mode = 'search'; loadSearchFetch(1); }
    });
    $id('empCreateForm').addEventListener('submit', createEmployee);
    loadLookups();
    loadEmployeesXHR(1);
})();
