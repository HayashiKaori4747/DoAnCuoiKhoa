$(function(){
    let table;

    if (!isAdmin()) {
        alert('Chỉ quản trị viên mới vào được màn hình này.');
        window.location.href = '/orders-list';
        return;
    }

    function badge(text, bg, color){
        return $('<span>').text(text).css({
            background: bg, color: color, padding: '2px 10px',
            borderRadius: '10px', fontSize: '12px', whiteSpace: 'nowrap'
        }).prop('outerHTML');
    }

    function renderRoles(data){
        if ($.fn.DataTable.isDataTable('#roles-table')) {
            $('#roles-table').DataTable().clear().destroy();
        }
        table = $('#roles-table').DataTable({
            data: data,
            columns: [
                { data: null, title: 'STT', render: (d,t,r,meta) => meta.row + 1 },
                {
                    data: 'code', title: 'Mã',
                    render: function(code, type, row){
                        if (type !== 'display') return code;
                        const ma = '<code>' + escapeHtml(code) + '</code>';
                        return row.isSystem
                            ? ma + ' ' + badge('hệ thống', '#d6eaf8', '#1f618d')
                            : ma;
                    }
                },
                { data: 'name', title: 'Tên hiển thị', render: renderText },
                { data: 'description', title: 'Mô tả', render: renderText },
                {
                    data: 'userCount', title: 'Số tài khoản',
                    render: function(n){
                        return n > 0 ? String(n) : '<span style="color:#95a5a6">0</span>';
                    }
                },
                {
                    data: null, title: 'Thao tác', orderable: false,
                    render: function(row){
                        let html = '<button class="btn-edit" data-id="' + row.id + '">Sửa</button>';

                        if (!row.isSystem && row.userCount === 0) {
                            html += ' <button class="btn-delete" data-id="' + row.id + '">Xoá</button>';
                        }
                        return html;
                    }
                }
            ],
            searching: false,
            paging: false,
            info: false,
            language: {
                emptyTable: "Chưa có vai trò nào",
                loadingRecords: "Đang tải..."
            }
        });
    }

    function fetchRoles(){
        return fetch('/api/roles')
            .then(res => res.json())
            .then(result => {
                if (!result.success) throw new Error(result.message);
                renderRoles(result.data || []);
            })
            .catch(err => {
                renderRoles([]);
                alert(err.message || 'Không tải được danh sách vai trò.');
            });
    }

    fetchRoles();

    function dialog(title, bodyBuilder, onSave){
        openDialog({
            title: title,
            build: bodyBuilder,
            onSave: onSave,
            onSuccess: fetchRoles
        });
    }

    const CODE_HINT =
        'Mã chỉ gồm chữ cái, chữ số và dấu gạch dưới, bắt đầu bằng chữ cái. ' +
        'Đây là chuỗi đi thẳng vào phân quyền nên KHÔNG được có dấu phẩy - ' +
        'phần kiểm tra quyền tách danh sách vai trò bằng dấu phẩy, một mã có dấu phẩy ' +
        'sẽ bị hiểu thành hai vai trò khác nhau.';

    $('#btn-create').on('click', function(){
        dialog('Tạo vai trò mới', function(box){
            box.append(dialogInput('r-code', 'Mã vai trò (ví dụ: Accounting)', 'text'));
            box.append(dialogInput('r-name', 'Tên hiển thị (ví dụ: Kế toán)', 'text'));
            box.append(dialogInput('r-desc', 'Mô tả', 'text'));
            box.append(dialogHint(CODE_HINT));
            box.append(dialogHint(
                'LƯU Ý: vai trò mới tạo ra CHƯA gắn với quyền nào trên API. ' +
                'Nó dùng được ngay để nhóm và đánh dấu tài khoản, nhưng muốn nó thực sự ' +
                'mở/khoá được chức năng thì phải khai mã này trong phân quyền của API ' +
                'tương ứng (Domain/Constants/RoleCode.cs).'));
        }, function(msg){
            const code = $('#r-code').val().trim();
            const name = $('#r-name').val().trim();

            if (!/^[A-Za-z][A-Za-z0-9_]*$/.test(code)) {
                msg.text('Mã vai trò không hợp lệ. ' + CODE_HINT);
                return false;
            }
            if (!name) {
                msg.text('Vui lòng nhập tên hiển thị.');
                return false;
            }

            return dialogApi('/api/roles', {
                code: code,
                name: name,
                description: $('#r-desc').val().trim()
            }, msg, 'POST');
        });
    });

    $('#roles-table').on('click', '.btn-edit', function(e){
        e.stopPropagation();
        const id = $(this).data('id');
        const row = table.rows().data().toArray().find(r => r.id === id);
        if (!row) return;

        dialog('Sửa vai trò: ' + row.code, function(box){
            const codeInput = dialogInput('e-code', 'Mã vai trò', 'text', row.code);
            box.append(codeInput);

            if (row.isSystem) {
                codeInput.find('input').prop('disabled', true)
                         .css({ background:'#f4f6f7', color:'#7f8c8d' });
            }

            box.append(dialogInput('e-name', 'Tên hiển thị', 'text', row.name));
            box.append(dialogInput('e-desc', 'Mô tả', 'text', row.description));

            if (row.isSystem) {
                box.append(dialogHint(
                    'Đây là vai trò HỆ THỐNG: mã không đổi được vì phần phân quyền của ' +
                    'các API đang tham chiếu trực tiếp tới nó. Tên hiển thị và mô tả thì ' +
                    'đổi thoải mái - chúng không tham gia vào việc kiểm quyền.'));
            } else {
                box.append(dialogHint(CODE_HINT));
            }
        }, function(msg){
            const code = row.isSystem ? row.code : $('#e-code').val().trim();
            const name = $('#e-name').val().trim();

            if (!/^[A-Za-z][A-Za-z0-9_]*$/.test(code)) {
                msg.text('Mã vai trò không hợp lệ. ' + CODE_HINT);
                return false;
            }
            if (!name) {
                msg.text('Vui lòng nhập tên hiển thị.');
                return false;
            }

            return dialogApi('/api/roles/' + id, {
                code: code,
                name: name,
                description: $('#e-desc').val().trim()
            }, msg);
        });
    });

    $('#roles-table').on('click', '.btn-delete', function(e){
        e.stopPropagation();
        const id = $(this).data('id');
        const row = table.rows().data().toArray().find(r => r.id === id);
        if (!row) return;

        if (!confirm('Xoá vai trò "' + row.name + '"?\n\n' +
                     'Hiện không có tài khoản nào giữ vai trò này nên xoá được. ' +
                     'Thao tác này không hoàn tác được.')) {
            return;
        }

        fetch('/api/roles/' + id, { method: 'DELETE' })
            .then(res => res.json())
            .then(result => {
                alert(result.message || (result.success ? 'Đã xoá' : 'Không xoá được vai trò.'));
                if (result.success) fetchRoles();
            })
            .catch(() => alert('Lỗi kết nối server.'));
    });
});
