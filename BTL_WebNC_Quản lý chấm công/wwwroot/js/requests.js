(() => {
    'use strict';
    const app = document.getElementById('request-app');
    if (!app) return;
    const search = document.getElementById('request-search');
    const rows = document.getElementById('request-rows');
    const pages = document.getElementById('request-pages');
    const summary = document.getElementById('request-summary');
    const message = document.getElementById('request-message');
    const create = document.getElementById('request-create');
    const token = document.querySelector('#request-token input').value;
    let filters = new URLSearchParams(new FormData(search));
    let page = Number(app.dataset.page) || 1;
    let version = 0;

    function notify(text, error = false) {
        message.textContent = text;
        message.className = `alert alert-${error ? 'danger' : 'success'}`;
        message.hidden = false;
    }
    function cell(row, value) {
        const td = document.createElement('td');
        td.textContent = value ?? '';
        row.append(td);
        return td;
    }
    function link(parent, text, url, id) {
        const a = document.createElement('a');
        const target = new URL(url, location.href);
        target.searchParams.set('id', id);
        a.href = target.href;
        a.className = 'btn btn-outline-secondary btn-sm me-1';
        a.textContent = text;
        parent.append(a);
    }
    function button(parent, text, style, callback) {
        const b = document.createElement('button');
        b.type = 'button';
        b.className = `btn btn-${style} btn-sm me-1`;
        b.textContent = text;
        b.addEventListener('click', () => callback(b));
        parent.append(b);
        return b;
    }
    function render(data) {
        page = data.page;
        rows.replaceChildren();
        for (const item of data.items) {
            const row = document.createElement('tr');
            cell(row, item.employee);
            cell(row, item.period);
            const labels = { Pending: 'Chờ duyệt', Approved: 'Đã duyệt', Rejected: 'Từ chối' };
            const colors = { Pending: 'warning', Approved: 'success', Rejected: 'danger' };
            const badge = document.createElement('span');
            badge.className = `badge bg-${colors[item.status] || 'secondary'}`;
            badge.textContent = labels[item.status] || item.status;
            cell(row, '').append(badge);
            cell(row, item.approver);
            const actions = cell(row, '');
            link(actions, 'Chi tiết', app.dataset.detailsUrl, item.id);
            if (item.canEdit) {
                link(actions, 'Sửa', app.dataset.editUrl, item.id);
                button(actions, 'Xóa', 'outline-danger', b => mutate(app.dataset.deleteUrl, item.id, 'Xóa đơn đang chờ duyệt này?', b));
            }
            if (item.canReview) {
                button(actions, 'Duyệt', 'success', b => mutate(app.dataset.approveUrl, item.id, 'Phê duyệt đơn này?', b));
                button(actions, 'Từ chối', 'danger', b => mutate(app.dataset.rejectUrl, item.id, 'Từ chối đơn này?', b));
            }
            rows.append(row);
        }
        if (!data.items.length) {
            const row = document.createElement('tr');
            cell(row, 'Không có yêu cầu phù hợp.').colSpan = 5;
            rows.append(row);
        }
        summary.textContent = `${data.totalCount} yêu cầu · Trang ${data.page}/${data.totalPages}`;
        pages.replaceChildren();
        if (page > 1) button(pages, 'Trang trước', 'outline-secondary', () => load(page - 1));
        if (page < data.totalPages) button(pages, 'Trang sau', 'outline-secondary', () => load(page + 1));
    }

    // XMLHttpRequest: tải bảng lần đầu và phân trang, nhận JSON từ controller.
    function getXhr(url) {
        return new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest();
            xhr.open('GET', url);
            xhr.responseType = 'json';
            xhr.timeout = 15000;
            xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
            xhr.onload = () => xhr.status === 200 && Array.isArray(xhr.response?.items)
                ? resolve(xhr.response) : reject(new Error('Không tải được đơn. Kiểm tra phiên đăng nhập và thử lại.'));
            xhr.onerror = () => reject(new Error('Lỗi kết nối. Vui lòng thử lại.'));
            xhr.ontimeout = () => reject(new Error('Yêu cầu quá thời gian. Vui lòng thử lại.'));
            xhr.send();
        });
    }
    async function load(targetPage = 1, useFetch = false) {
        const current = ++version;
        const query = new URLSearchParams(filters);
        query.set('page', targetPage);
        query.set('json', 'true');
        const url = `${app.dataset.listUrl}?${query}`;
        summary.textContent = 'Đang tải dữ liệu...';
        app.querySelector('table').setAttribute('aria-busy', 'true');
        try {
            let data;
            if (useFetch) {
                // fetch: tìm kiếm, lọc trạng thái và sắp xếp.
                const controller = new AbortController();
                const timeout = setTimeout(() => controller.abort(), 15000);
                try {
                    const response = await fetch(url, { signal: controller.signal, headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                    if (!response.ok || !response.headers.get('content-type')?.includes('application/json'))
                        throw new Error('Không tìm kiếm được đơn. Kiểm tra phiên đăng nhập và thử lại.');
                    data = await response.json();
                } finally { clearTimeout(timeout); }
            } else data = await getXhr(url);
            // Phản hồi cũ không được ghi đè kết quả tìm kiếm mới hơn.
            if (current === version) render(data);
        } catch (error) {
            if (current === version) {
                rows.replaceChildren();
                pages.replaceChildren();
                summary.textContent = 'Bấm Lọc để tải lại danh sách.';
                notify(error.name === 'AbortError' ? 'Yêu cầu quá thời gian.' : error.message, true);
            }
        } finally {
            if (current === version) app.querySelector('table').setAttribute('aria-busy', 'false');
        }
    }
    search.addEventListener('submit', event => {
        event.preventDefault();
        filters = new URLSearchParams(new FormData(search));
        message.hidden = true;
        load(1, true);
    });

    function errorText(xhr) {
        if (xhr.responseJSON?.errors) return Object.values(xhr.responseJSON.errors).flat().join(' ');
        if (xhr.responseJSON?.message) return xhr.responseJSON.message;
        if (xhr.status === 409) return xhr.responseText || 'Đơn vừa được người khác xử lý. Hãy tải lại danh sách.';
        if (xhr.status === 401 || xhr.status === 403) return 'Phiên đăng nhập đã hết hạn hoặc bạn không có quyền thực hiện.';
        if (xhr.status === 404) return 'Đơn không còn tồn tại hoặc không thuộc quyền truy cập của bạn.';
        return 'Không xác nhận được kết quả. Vui lòng tải lại danh sách để kiểm tra trước khi thử lại.';
    }
    // jQuery AJAX: tạo đơn trong modal; form gửi kèm anti-forgery token.
    if (create) {
        const errors = document.getElementById('request-errors');
        $(create).on('submit', function (event) {
            event.preventDefault();
            if (!$(create).valid()) return;
            const submit = create.querySelector('[type="submit"]');
            if (submit.disabled) return;
            submit.disabled = true;
            errors.hidden = true;
            $.ajax({ url: create.action, method: 'POST', data: $(create).serialize(), dataType: 'json', timeout: 15000 })
                .done(data => {
                    bootstrap.Modal.getOrCreateInstance(document.getElementById('request-modal')).hide();
                    create.reset();
                    $(create).validate().resetForm();
                    notify(data.message);
                    load(page);
                }).fail(xhr => {
                    errors.textContent = errorText(xhr);
                    errors.hidden = false;
                }).always(() => { submit.disabled = false; });
        });
    }
    // jQuery AJAX: xóa, duyệt và từ chối; server vẫn quyết định quyền và trạng thái.
    function mutate(url, id, confirmation, source) {
        if (!confirm(confirmation)) return;
        const controls = source.parentElement.querySelectorAll('button');
        controls.forEach(b => { b.disabled = true; });
        $.ajax({ url, method: 'POST', data: { id, __RequestVerificationToken: token }, dataType: 'json', timeout: 15000 })
            .done(data => { notify(data.message); load(page); })
            .fail(xhr => { notify(errorText(xhr), true); load(page); })
            .always(() => { controls.forEach(b => { b.disabled = false; }); });
    }
    load(page);
})();
