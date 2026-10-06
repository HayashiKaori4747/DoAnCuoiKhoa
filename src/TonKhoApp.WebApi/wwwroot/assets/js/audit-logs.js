$(function(){
    if (!isAdmin()) {
        alert('Chỉ quản trị viên mới vào được màn hình này.');
        window.location.href = '/orders-list';
        return;
    }

    const PAGE_SIZE = 20;
    let page = 1;
    let totalPages = 1;

    const ACTION_STYLE = {
        PRICE_CHANGE:   ['#fdebd0', '#b9770e'],
        LOCK:           ['#fadbd8', '#c0392b'],
        UNLOCK:         ['#d5f5e3', '#1e8449'],
        ROLE_GRANT:     ['#d6eaf8', '#1f618d'],
        ROLE_REVOKE:    ['#fadbd8', '#c0392b'],
        PASSWORD_RESET: ['#fdebd0', '#b9770e'],
        PASSWORD_CHANGE:['#fdebd0', '#b9770e'],
        LOGIN_FAILED:   ['#fadbd8', '#c0392b'],
        ORDER_CANCEL:   ['#fadbd8', '#c0392b'],
        STOCK_ADJUST:   ['#fdebd0', '#b9770e'],
        STOCK_REVERSAL: ['#e8daef', '#6c3483'],
        DELETE:         ['#fadbd8', '#c0392b']
    };

    function actionBadge(action, text){
        const color = ACTION_STYLE[action] || ['#eaecee', '#566573'];
        return $('<span>').text(text || action).css({
            background: color[0], color: color[1], padding: '2px 10px',
            borderRadius: '10px', fontSize: '12px', whiteSpace: 'nowrap'
        }).prop('outerHTML');
    }

    function loadActions(){
        return fetch('/api/audit-logs/actions')
            .then(res => res.json())
            .then(result => {
                if (!result.success) return;
                const sel = $('#f-action');
                (result.data || []).forEach(function(a){
                    sel.append($('<option>').val(a).text(a));
                });
            })
            .catch(function(){   });
    }

    function renderDiff(before, after){
        const parts = [];
        if (before) parts.push('trước: ' + before);
        if (after)  parts.push('sau:   ' + after);
        if (parts.length === 0) return '';
        return '<div class="audit-diff">' + escapeHtml(parts.join('\n')) + '</div>';
    }

    function render(rows){
        const body = $('#audit-body');

        if (!rows || rows.length === 0) {
            body.html('<p style="color:#7f8c8d">Không có bản ghi nào khớp bộ lọc.</p>');
            return;
        }

        let html = '<div style="overflow-x:auto"><table class="audit-table"><thead><tr>' +
                   '<th>Thời điểm</th><th>Hành động</th><th>Đối tượng</th>' +
                   '<th>Người thực hiện</th><th>IP</th><th>Chi tiết</th>' +
                   '</tr></thead><tbody>';

        rows.forEach(function(r){
            const doiTuong = escapeHtml(r.entityName) +
                             (r.entityId ? ' <code>' + escapeHtml(r.entityId) + '</code>' : '');

            html += '<tr>' +
                '<td style="white-space:nowrap">' + escapeHtml(formatDateTime(r.createdAt)) + '</td>' +
                '<td>' + actionBadge(r.action, r.actionText) + '</td>' +
                '<td style="white-space:nowrap">' + doiTuong + '</td>' +
                '<td>' + escapeHtml(r.userName) + '</td>' +
                '<td style="white-space:nowrap;color:#7f8c8d">' + escapeHtml(r.ipAddress || '') + '</td>' +
                '<td>' + escapeHtml(r.description || '') + renderDiff(r.beforeJson, r.afterJson) + '</td>' +
                '</tr>';
        });

        html += '</tbody></table></div>';
        body.html(html);
    }

    function currentFilter(){
        return {
            page: page,
            pageSize: PAGE_SIZE,
            action: $('#f-action').val() || '',
            entityName: $('#f-entity').val() || '',
            entityId: $('#f-entity-id').val().trim(),
            fromDate: $('#f-from').val() || '',
            toDate: $('#f-to').val() || ''
        };
    }

    function fetchLogs(){
        const f = currentFilter();
        const query = new URLSearchParams();

        Object.keys(f).forEach(function(k){
            if (f[k] !== '' && f[k] !== null && f[k] !== undefined) query.set(k, f[k]);
        });

        $('#audit-body').text('Đang tải...');

        return fetch('/api/audit-logs?' + query.toString())
            .then(res => res.json())
            .then(result => {
                if (!result.success) throw new Error(result.message);

                render(result.data || []);
                totalPages = result.totalPages || 1;

                $('#page-info').text('Trang ' + result.page + '/' + totalPages +
                                     '  •  ' + result.totalCount + ' bản ghi');
                $('#btn-prev').prop('disabled', result.page <= 1);
                $('#btn-next').prop('disabled', result.page >= totalPages);
            })
            .catch(err => {
                $('#audit-body').html('<p style="color:#c0392b">' +
                    escapeHtml(err.message || 'Không tải được nhật ký.') + '</p>');
            });
    }

    $('#btn-filter').on('click', function(){
        page = 1;
        fetchLogs();
    });

    $('#btn-reset').on('click', function(){
        $('#f-action, #f-entity').val('');
        $('#f-entity-id, #f-from, #f-to').val('');
        page = 1;
        fetchLogs();
    });

    $('#btn-prev').on('click', function(){
        if (page > 1) { page--; fetchLogs(); }
    });

    $('#btn-next').on('click', function(){
        if (page < totalPages) { page++; fetchLogs(); }
    });

    $('#f-entity-id').on('keypress', function(e){
        if (e.which === 13) $('#btn-filter').click();
    });

    loadActions().then(fetchLogs);
});
