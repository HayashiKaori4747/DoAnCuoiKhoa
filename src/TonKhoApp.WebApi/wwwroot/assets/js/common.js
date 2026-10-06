(function installTokenRefresh() {
    const originalFetch = window.fetch.bind(window);

    let refreshing = null;

    function refreshOnce() {
        if (!refreshing) {
            refreshing = originalFetch('/api/auth/refresh', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: '{}'
            })
            .then(res => res.ok)
            .catch(() => false)
            .finally(() => { refreshing = null; });
        }
        return refreshing;
    }

    window.fetch = async function (input, init) {
        const res = await originalFetch(input, init);

        if (res.status !== 401) return res;

        const url = String((input && input.url) || input || '');

        if (url.includes('/api/auth/refresh') ||
            url.includes('/api/auth/login') ||
            url.includes('/api/auth/logout')) {
            return res;
        }

        if (!url.includes('/api/')) return res;

        const ok = await refreshOnce();
        if (!ok) {
            window.location.href = '/login';
            return res;
        }

        return originalFetch(input, init);
    };
})();

$(function(){

    $(".user-btn").on('click', function(e){
        e.stopPropagation();
        $(".user-dropdown-content").slideToggle(300);
    });
    $(document).on('click', function(){
        $(".user-dropdown-content").slideUp(200);
    });
    $(".user-dropdown-content").on('click', function(e){
        e.stopPropagation();
    });

    function getCookie(name) {
        const match = document.cookie.split('; ').find(row => row.startsWith(name + '='));
        return match ? decodeURIComponent(match.split('=')[1]) : null;
    }

    const userInfoRaw = getCookie('user_info');

    if (!userInfoRaw) {
        window.location.href = '/login';
    } else {
        try {
            const userInfo = JSON.parse(userInfoRaw);
            $('#user-fullname').text(userInfo.fullName || userInfo.userId);

            window.USER_ROLES = Array.isArray(userInfo.roles) ? userInfo.roles : [];

            buildMenu();

            $('<a>')
                .attr('href', '#')
                .attr('id', 'open-change-password')
                .text('Đổi mật khẩu')
                .insertBefore($('.user-dropdown-content #logout'));
        } catch (e) {
            window.location.href = '/login';
        }
    }

    function buildMenu() {
        const MENU = [
            { href: '/dashboard',               text: 'Dashboard',     show: always },
            { href: '/orders-list',             text: 'DS Đơn hàng',   show: always },
            { href: '/order-detail?mode=new',   text: 'Tạo đơn hàng',  show: canEditOrder },
            { href: '/products-list',           text: 'DS Sản phẩm',   show: always },
            { href: '/vendors-list',            text: 'Nhà cung cấp',  show: always },
            { href: '/stock',                   text: 'Tồn kho',       show: always },
            { href: '/users-list',              text: 'Tài khoản',     show: isAdmin },
            { href: '/roles-list',              text: 'Vai trò',       show: isAdmin },
            { href: '/audit-logs',              text: 'Nhật ký',       show: isAdmin }
        ];

        const here = window.location.pathname.toLowerCase();
        const $ul = $('.menu-btn ul').empty();

        MENU.forEach(function (m) {
            if (!m.show()) return;
            const $a = $('<a>').attr('href', m.href).text(m.text);
            if (m.href.split('?')[0].toLowerCase() === here) $a.addClass('active');
            $ul.append($('<li>').append($a));
        });
    }

    function always() { return true; }

    $('.user-dropdown-content').on('click', '#open-change-password', function(e){
        e.preventDefault();
        $('.user-dropdown-content').slideUp(200);
        openChangePasswordDialog();
    });

    function openChangePasswordDialog(){
        openDialog({
            title: 'Đổi mật khẩu',
            build: function(box){
                box.append(dialogInput('cp-current', 'Mật khẩu hiện tại', 'password'));
                box.append(dialogInput('cp-new', 'Mật khẩu mới (tối thiểu 6 ký tự)', 'password'));
                box.append(dialogInput('cp-confirm', 'Nhập lại mật khẩu mới', 'password'));
                box.append(dialogHint('Đổi xong thì mật khẩu cũ hết hiệu lực ngay. ' +
                                      'Lần đăng nhập sau hãy dùng mật khẩu mới.'));
            },
            onSave: function(msg){
                const currentPassword = $('#cp-current').val();
                const newPassword = $('#cp-new').val();
                const confirmPassword = $('#cp-confirm').val();

                if (!currentPassword || !newPassword || !confirmPassword) {
                    msg.text('Vui lòng điền đủ 3 ô.');
                    return false;
                }
                if (newPassword.length < 6) {
                    msg.text('Mật khẩu mới phải có ít nhất 6 ký tự.');
                    return false;
                }
                if (newPassword !== confirmPassword) {
                    msg.text('Mật khẩu xác nhận không khớp.');
                    return false;
                }
                return dialogApi('/api/auth/change-password',
                                 { currentPassword, newPassword, confirmPassword }, msg);
            }
        });
    }

    $('#logout').on('click', async function(e){
        e.preventDefault();
        try {
            await fetch('/api/auth/logout', { method: 'POST' });
        } catch(err) {   }
        window.location.href = '/login';
    });
});

function hasRole() {
    const mine = (window.USER_ROLES || []).map(r => String(r).toLowerCase());
    return Array.prototype.some.call(arguments, r => mine.includes(String(r).toLowerCase()));
}

function isAdmin() {
    return hasRole('Admin');
}

function canManageCatalog() {
    return hasRole('Admin', 'Purchasing');
}

function canConfirmOrder() {
    return hasRole('Admin', 'Purchasing');
}

function canManageStock() {
    return hasRole('Admin', 'Warehouse');
}

function canEditOrder() {
    return hasRole('Admin', 'Purchasing');
}

const ROLE_LABELS = {
    admin:      'Quản trị viên',
    purchasing: 'Nhân viên mua hàng',
    warehouse:  'Nhân viên kho',
    user:       'Nhân viên'
};

function roleLabel(role) {
    return ROLE_LABELS[String(role || '').toLowerCase()] || role || '';
}

function escapeHtml(value) {
    if (value === null || value === undefined) return '';
    return String(value)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function renderText(data, type) {
    return type === 'display' ? escapeHtml(data) : data;
}

const PAGE_SIZE = 200;
const MAX_PAGES = 25;

async function fetchAllPages(baseUrl, params) {
    const query = new URLSearchParams(params || {});
    query.set('pageSize', PAGE_SIZE);

    const all = [];
    let page = 1;

    while (page <= MAX_PAGES) {
        query.set('page', page);

        const res = await fetch(baseUrl + '?' + query.toString());
        const result = await res.json();

        if (!res.ok || result.success === false) {
            throw new Error(result.message || 'Không tải được dữ liệu.');
        }

        const rows = result.data || [];
        all.push.apply(all, rows);

        const total = typeof result.totalCount === 'number' ? result.totalCount : all.length;
        if (all.length >= total || rows.length === 0) break;

        page++;
    }

    if (page > MAX_PAGES) {
        console.warn('fetchAllPages: đã chạm trần ' + MAX_PAGES + ' trang cho ' + baseUrl +
                     '. Danh sách đang hiển thị THIẾU - cần chuyển sang phân trang phía server.');
    }

    return all;
}

function lineAmount(quantity, unitPrice) {
    var q = Math.round((Number(quantity) || 0) * 1000);
    var p = Math.round((Number(unitPrice) || 0) * 100);
    return Math.round(q * p / 1000) / 100;
}

function formatAmount(num) {
    if (!num && num !== 0) return '';
    return Number(num).toLocaleString('en-US');
}

function formatDateTime(value, emptyText) {
    if (!value) return emptyText || '';
    const d = new Date(/[Zz]|[+-]\d{2}:?\d{2}$/.test(value) ? value : value + 'Z');
    if (isNaN(d)) return '';
    const pad = n => String(n).padStart(2, '0');
    return pad(d.getDate()) + '/' + pad(d.getMonth() + 1) + '/' + d.getFullYear() +
           ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes());
}

function parseAmount(str) {
    if (!str) return 0;
    return parseFloat(String(str).replace(/,/g, '')) || 0;
}

function openDialog(options) {
    $('#app-dialog-overlay').remove();

    const overlay = $('<div>').attr('id', 'app-dialog-overlay').css({
        position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.45)', display: 'flex',
        alignItems: 'center', justifyContent: 'center', zIndex: 3000
    });
    const box = $('<div>').css({
        background: '#fff', borderRadius: '6px', padding: '24px', width: '380px',
        maxWidth: '92vw', boxShadow: '0 8px 24px rgba(0,0,0,0.2)'
    });
    const msg = $('<div>').css({ color: '#c0392b', fontSize: '13px', minHeight: '18px', margin: '6px 0' });

    const btnSave = $('<button>').text('Lưu').css({
        background: '#2471a3', color: '#fff', border: 'none', borderRadius: '4px',
        padding: '8px 16px', cursor: 'pointer', marginRight: '8px'
    });
    const btnCancel = $('<button>').text('Huỷ').css({
        background: '#7f8c8d', color: '#fff', border: 'none', borderRadius: '4px',
        padding: '8px 16px', cursor: 'pointer'
    });

    btnCancel.on('click', function(){ overlay.remove(); });
    btnSave.on('click', async function(){
        msg.text('');
        btnSave.prop('disabled', true).text('Đang lưu...');
        const ok = await options.onSave(msg);
        if (ok) {
            overlay.remove();
            if (options.onSuccess) options.onSuccess();
        } else {
            btnSave.prop('disabled', false).text('Lưu');
        }
    });

    box.append($('<h3>').text(options.title).css({ marginBottom: '16px' }));
    options.build(box);
    box.append(msg).append(btnSave).append(btnCancel);
    overlay.append(box).appendTo('body');

    overlay.on('click', function(e){ if (e.target === this) overlay.remove(); });
    box.on('keydown', function(e){ if (e.key === 'Enter') btnSave.click(); });
    box.find('input').first().focus();
}

function dialogInput(id, label, type, value) {
    return $('<div>').css({ marginBottom: '12px' })
        .append($('<label>').text(label).css({ display: 'block', marginBottom: '4px', fontSize: '14px' }))
        .append($('<input>').attr({ type: type || 'text', id: id }).val(value || '').css({
            width: '100%', padding: '7px', border: '1px solid #ccc', borderRadius: '4px'
        }));
}

function dialogHint(text) {
    return $('<small>').css({ display: 'block', color: '#7f8c8d', fontSize: '12px', lineHeight: '1.5' })
        .text(text);
}

async function dialogApi(url, body, msg, method) {
    try {
        const res = await fetch(url, {
            method: method || 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });
        const result = await res.json();
        if (res.ok && result.success) {
            alert(result.message || 'Thành công');
            return true;
        }
        msg.text(result.message || 'Thao tác không thành công.');
        return false;
    } catch (err) {
        msg.text('Lỗi kết nối server.');
        return false;
    }
}
