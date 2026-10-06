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

    function statusBadge(isActive){
        return isActive
            ? badge('Đang dùng', '#d5f5e3', '#1e8449')
            : badge('Đã khoá', '#fadbd8', '#c0392b');
    }

    const ROLE_STYLE = {
        admin:      ['#d6eaf8', '#1f618d'],
        purchasing: ['#fdebd0', '#b9770e'],
        warehouse:  ['#d5f5e3', '#1e8449'],
        user:       ['#eaecee', '#566573']
    };

    function roleBadges(roles){
        const list = Array.isArray(roles) ? roles : (roles ? [roles] : []);
        if (list.length === 0) {
            return badge('(chưa cấp quyền)', '#fadbd8', '#c0392b');
        }
        return list.map(function(r){
            const color = ROLE_STYLE[String(r).toLowerCase()] || ROLE_STYLE.user;
            return badge(roleLabel(r), color[0], color[1]);
        }).join(' ');
    }

    const ROLE_VALUES = ['User', 'Warehouse', 'Purchasing', 'Admin'];

    function roleChecks(idPrefix, currentRoles){
        const mine = (currentRoles || []).map(r => String(r).toLowerCase());

        const box = $('<div>').css({
            border: '1px solid #ccc', borderRadius: '4px', padding: '8px'
        });

        ROLE_VALUES.forEach(function(r){
            const id = idPrefix + '-' + r.toLowerCase();
            const line = $('<label>').css({
                display: 'block', marginBottom: '4px', fontSize: '14px', cursor: 'pointer'
            });

            $('<input>')
                .attr({ type: 'checkbox', id: id, value: r })
                .addClass(idPrefix + '-role')
                .prop('checked', mine.includes(r.toLowerCase()))
                .css({ marginRight: '6px' })
                .appendTo(line);

            line.append(document.createTextNode(roleLabel(r)));
            box.append(line);
        });

        return $('<div>').css({marginBottom:'12px'})
            .append($('<label>').text('Vai trò (chọn được nhiều)')
                                .css({display:'block',marginBottom:'4px',fontSize:'14px'}))
            .append(box);
    }

    function readRoles(idPrefix){
        return $('.' + idPrefix + '-role:checked').map(function(){
            return this.value;
        }).get();
    }

    const ROLE_HINT =
        'Chọn được NHIỀU vai trò - quyền là hợp của tất cả.  •  ' +
        'Nhân viên: chỉ xem, không thêm/sửa/xoá gì.  •  ' +
        'Nhân viên kho: thêm quyền nhập kho, xuất kho, điều chỉnh tồn, đảo bút toán.  •  ' +
        'Nhân viên mua hàng: thêm quyền lập/sửa/xoá/huỷ đơn, xác nhận đơn, quản lý sản phẩm và NCC.  •  ' +
        'Quản trị viên: toàn quyền, kể cả quản lý tài khoản và xem nhật ký.';

    function renderLastLogin(value){
        return value
            ? escapeHtml(formatDateTime(value))
            : '<span style="color:#95a5a6">chưa đăng nhập</span>';
    }

    function renderUsers(data){
        if ($.fn.DataTable.isDataTable('#users-table')) {
            $('#users-table').DataTable().clear().destroy();
        }
        table = $('#users-table').DataTable({
            data: data,
            columns: [
                { data: null, title: 'STT', render: (d,t,r,meta) => meta.row + 1 },
                { data: 'userId', title: 'Tên đăng nhập', render: renderText },
                { data: 'fullName', title: 'Họ tên', render: renderText },
                { data: 'roles', title: 'Vai trò', orderable: false, render: roleBadges },
                { data: 'isActive', title: 'Trạng thái', render: statusBadge },
                { data: 'lastLoginAt', title: 'Đăng nhập gần nhất', render: renderLastLogin },
                {
                    data: null, title: 'Thao tác', orderable: false,
                    render: function(row){
                        return '<button class="btn-edit" data-id="' + row.id + '">Sửa</button> ' +
                               '<button class="btn-reset" data-id="' + row.id + '">Đặt lại MK</button>';
                    }
                }
            ],
            createdRow: function(row, rowData){
                if (!rowData.isActive) $(row).css('opacity', '0.6');
            },
            searching: false,
            language: {
                emptyTable: "Không có tài khoản nào",
                info: "Hiển thị _START_ đến _END_ của _TOTAL_ dòng",
                infoEmpty: "Hiển thị 0 đến 0 của 0 dòng",
                lengthMenu: "Hiển thị _MENU_ dòng",
                loadingRecords: "Đang tải...",
                zeroRecords: "Không tìm thấy tài khoản phù hợp",
                paginate: { first:"Đầu tiên", last:"Cuối cùng", next:"Sau", previous:"Trước" }
            }
        });
    }

    function fetchUsers(keyword){
        fetchAllPages('/api/users', keyword ? { search: keyword } : {})
            .then(renderUsers)
            .catch(err => {
                renderUsers([]);
                alert(err.message || 'Không tải được danh sách tài khoản.');
            });
    }

    fetchUsers();

    $("#btn-search").on("click", function(){
        fetchUsers($("#search-input").val().trim());
    });
    $("#search-input").on("keypress", function(e){
        if (e.which === 13) $("#btn-search").click();
    });

    $("#btn-create").on("click", function(){
        dialog('Tạo tài khoản mới', function(box){
            box.append(dialogInput('c-userid', 'Tên đăng nhập (tối thiểu 2 ký tự)', 'text'));
            box.append(dialogInput('c-fullname', 'Họ tên', 'text'));
            box.append(dialogInput('c-password', 'Mật khẩu (tối thiểu 6 ký tự)', 'password'));
            box.append(dialogInput('c-confirm', 'Nhập lại mật khẩu', 'password'));
            box.append(roleChecks('c', ['User']));
            box.append(dialogHint(ROLE_HINT));
            box.append(dialogHint('Tên đăng nhập là khoá định danh, ĐẶT RỒI KHÔNG ĐỔI ĐƯỢC - ' +
                                  'nó đã được các đơn hàng và bút toán kho tham chiếu tới. ' +
                                  'Báo mật khẩu cho nhân viên và nhắc họ tự đổi ngay lần đăng nhập đầu.'));
        }, function(msg){
            const userId = $('#c-userid').val().trim();
            const password = $('#c-password').val();
            const confirm = $('#c-confirm').val();

            if (userId.length < 2) {
                msg.text('Tên đăng nhập phải có ít nhất 2 ký tự.');
                return false;
            }
            if (!password || password.length < 6) {
                msg.text('Mật khẩu phải có ít nhất 6 ký tự.');
                return false;
            }
            if (password !== confirm) {
                msg.text('Mật khẩu xác nhận không khớp.');
                return false;
            }

            const roles = readRoles('c');
            if (roles.length === 0) {
                msg.text('Phải chọn ít nhất một vai trò.');
                return false;
            }

            return dialogApi('/api/users', {
                userId: userId,
                fullName: $('#c-fullname').val().trim(),
                password: password,
                roles: roles
            }, msg, 'POST');
        });
    });

    function dialog(title, bodyBuilder, onSave){
        openDialog({
            title: title,
            build: bodyBuilder,
            onSave: onSave,
            onSuccess: function(){ fetchUsers($("#search-input").val().trim()); }
        });
    }

    $('#users-table').on('click', '.btn-edit', function(e){
        e.stopPropagation();
        const id = $(this).data('id');
        const row = table.rows().data().toArray().find(u => u.id === id);
        if (!row) return;

        dialog('Sửa tài khoản: ' + row.userId, function(box){
            box.append(dialogInput('u-fullname', 'Họ tên', 'text', row.fullName));
            box.append(roleChecks('u', row.roles));
            box.append(dialogHint(ROLE_HINT));

            box.append(
                $('<label>').css({display:'flex',alignItems:'center',gap:'8px',cursor:'pointer',marginBottom:'6px'})
                    .append($('<input>').attr({type:'checkbox', id:'u-active'}).prop('checked', row.isActive)
                        .css({width:'18px',height:'18px',margin:0}))
                    .append($('<span>').text('Tài khoản còn hiệu lực'))
            );
            box.append(dialogHint('Bỏ tích = khoá tài khoản. Người này sẽ không đăng nhập được nữa, ' +
                                  'nhưng mọi đơn hàng và bút toán kho họ đã tạo vẫn giữ nguyên.'));
        }, function(msg){
            const roles = readRoles('u');

            if (roles.length === 0) {
                msg.text('Phải chọn ít nhất một vai trò. Muốn chặn truy cập thì bỏ tích "Tài khoản còn hiệu lực".');
                return false;
            }

            return dialogApi('/api/users/' + id, {
                fullName: $('#u-fullname').val().trim(),
                roles: roles,
                isActive: $('#u-active').is(':checked')
            }, msg);
        });
    });

    $('#users-table').on('click', '.btn-reset', function(e){
        e.stopPropagation();
        const id = $(this).data('id');
        const row = table.rows().data().toArray().find(u => u.id === id);
        if (!row) return;

        dialog('Đặt lại mật khẩu: ' + row.userId, function(box){
            box.append(dialogInput('r-new', 'Mật khẩu mới (tối thiểu 6 ký tự)', 'password'));
            box.append(dialogInput('r-confirm', 'Nhập lại mật khẩu mới', 'password'));
            box.append(dialogHint('Báo lại mật khẩu này cho nhân viên và nhắc họ tự đổi ngay sau khi đăng nhập ' +
                                  '(menu tên mình ở góc phải → Đổi mật khẩu).'));
        }, function(msg){
            const newPassword = $('#r-new').val();
            const confirmPassword = $('#r-confirm').val();

            if (!newPassword || newPassword.length < 6) {
                msg.text('Mật khẩu mới phải có ít nhất 6 ký tự.');
                return false;
            }
            if (newPassword !== confirmPassword) {
                msg.text('Mật khẩu xác nhận không khớp.');
                return false;
            }
            return dialogApi('/api/users/' + id + '/password', { newPassword, confirmPassword }, msg);
        });
    });
});
