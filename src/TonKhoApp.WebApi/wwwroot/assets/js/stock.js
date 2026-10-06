$(function(){
    let table;
    let stockRows = [];

    function renderStock(data){
        stockRows = data || [];

        if ($.fn.DataTable.isDataTable('#stock-table')){
            $('#stock-table').DataTable().clear().destroy();
        }

        table = $('#stock-table').DataTable({
            data: stockRows,
            columns: [
                { data: null, title: 'STT', render: function(data,type,row,meta){ return meta.row + 1; }},
                { data: 'item_CD', title: 'Mã sản phẩm', render: renderText },
                { data: 'item_name', title: 'Tên sản phẩm', render: renderText },
                { data: 'unit', title: 'Đơn vị', render: renderText },
                { data: 'stock', title: 'Số lượng', render: function(data){ return formatAmount(data); }},
                {
                    data: 'minQuantity', title: 'Định mức',
                    render: function(data, type, row){
                        if (!data) return '<span style="color:#95a5a6">—</span>';
                        return row.belowMinimum
                            ? '<span style="color:#c0392b;font-weight:600">' + formatAmount(data) + '</span>'
                            : formatAmount(data);
                    }
                },
                { data: 'price', title: 'Đơn giá', render: function(data){ return formatAmount(data); }},
                { data: 'stockValue', title: 'Giá trị tồn', render: function(data){ return formatAmount(data); }}
            ],
            createdRow: function(row, rowData){
                $(row).attr('data-product-code', rowData.item_CD).css('cursor', 'pointer');

                if (rowData.belowMinimum) {
                    $(row).css('background', '#fdf2f0');
                }
            },
            searching: false,
            language: {
                emptyTable: "Không có dữ liệu tồn kho",
                info: "Hiển thị _START_ đến _END_ của _TOTAL_ dòng",
                infoEmpty: "Hiển thị 0 đến 0 của 0 dòng",
                lengthMenu: "Hiển thị _MENU_ dòng",
                loadingRecords: "Đang tải...",
                zeroRecords: "Không tìm thấy dữ liệu phù hợp",
                paginate: { first:"Đầu tiên", last:"Cuối cùng", next:"Sau", previous:"Trước" }
            }
        });

        table.on('draw.dt', function(){
            table.column(0, {search:'applied', order:'applied'}).nodes().each(function(cell, i){
                cell.innerHTML = i + 1;
            });
        });
    }

    function fetchStock(keyword) {
        return fetchAllPages('/api/stock', keyword ? { search: keyword } : {})
            .then(renderStock)
            .catch(err => {
                renderStock([]);
                alert(err.message || 'Không tải được dữ liệu tồn kho.');
            });
    }

    function reload(){
        return fetchStock($("#search-input").val().trim());
    }

    fetchStock();

    $("#btn-search").on("click", reload);
    $("#search-input").on("keypress", function(event){
        if(event.which === 13) $("#btn-search").click();
    });

    $('#btn-issue, #btn-adjust').toggle(canManageStock());

    function productSelect(id){
        const select = $('<select>').attr('id', id).css({
            width:'100%', padding:'7px', border:'1px solid #ccc', borderRadius:'4px'
        });

        if (stockRows.length === 0) {
            select.append($('<option>').val('').text('(chưa có sản phẩm nào trong kho)'));
        }
        stockRows.forEach(function(r){
            select.append($('<option>').val(r.item_CD)
                .text(r.item_CD + ' - ' + r.item_name + '  (tồn: ' + formatAmount(r.stock) + ' ' + (r.unit || '') + ')'));
        });

        return $('<div>').css({marginBottom:'12px'})
            .append($('<label>').text('Sản phẩm').css({display:'block',marginBottom:'4px',fontSize:'14px'}))
            .append(select);
    }

    $('#btn-issue').on('click', function(){
        openDialog({
            title: 'Xuất kho',
            build: function(box){
                box.append(productSelect('i-item'));
                box.append(dialogInput('i-qty', 'Số lượng xuất', 'number'));
                box.append(dialogInput('i-note', 'Lý do xuất (kho nào, cho ai, phiếu nào)', 'text'));
                box.append(dialogHint('Xuất kho ghi một bút toán ÂM vào sổ cái và trừ vào tồn. ' +
                                      'Bút toán đã ghi thì không sửa/xoá được - nhập nhầm thì phải ' +
                                      'dùng Điều chỉnh tồn để nắn lại, và cả hai lượt đều nằm trong sổ.'));
            },
            onSave: function(msg){
                const itemCode = $('#i-item').val();
                const quantity = parseFloat($('#i-qty').val());

                if (!itemCode) { msg.text('Vui lòng chọn sản phẩm.'); return false; }
                if (isNaN(quantity) || quantity <= 0) {
                    msg.text('Số lượng xuất phải lớn hơn 0.');
                    return false;
                }

                const row = stockRows.find(r => r.item_CD === itemCode);
                if (row && quantity > row.stock) {
                    msg.text('Không đủ tồn kho: hiện còn ' + formatAmount(row.stock) + ' ' + (row.unit || '') + '.');
                    return false;
                }

                return dialogApi('/api/stock/issue',
                                 { itemCode: itemCode, quantity: quantity, note: $('#i-note').val().trim() || null },
                                 msg, 'POST');
            },
            onSuccess: reload
        });
    });

    $('#btn-adjust').on('click', function(){
        openDialog({
            title: 'Điều chỉnh tồn theo kiểm kê',
            build: function(box){
                box.append(productSelect('a-item'));
                box.append(dialogInput('a-qty', 'Số lượng THỰC TẾ đếm được', 'number'));
                box.append(dialogInput('a-note', 'Ghi chú (ai kiểm, ngày nào)', 'text'));
                box.append(dialogHint('Nhập số ĐẾM ĐƯỢC NGOÀI KHO, không phải phần chênh lệch. ' +
                                      'Hệ thống tự tính chênh lệch so với tồn đang có và ghi đúng ' +
                                      'phần đó vào sổ cái, nên luôn giải thích được vì sao số tồn đổi.'));
            },
            onSave: function(msg){
                const itemCode = $('#a-item').val();
                const actualQuantity = parseFloat($('#a-qty').val());

                if (!itemCode) { msg.text('Vui lòng chọn sản phẩm.'); return false; }
                if (isNaN(actualQuantity) || actualQuantity < 0) {
                    msg.text('Số lượng thực tế không được âm.');
                    return false;
                }

                return dialogApi('/api/stock/adjust',
                                 { itemCode: itemCode, actualQuantity: actualQuantity, note: $('#a-note').val().trim() || null },
                                 msg, 'POST');
            },
            onSuccess: reload
        });
    });

    const TYPE_STYLE = {
        IN:       ['#d5f5e3', '#1e8449'],
        OUT:      ['#fadbd8', '#c0392b'],
        ADJUST:   ['#fdebd0', '#b9770e'],
        REVERSAL: ['#e8daef', '#6c3483']
    };

    function typeBadge(type, text){
        const color = TYPE_STYLE[type] || ['#eaecee', '#566573'];
        return $('<span>').text(text || type).css({
            background: color[0], color: color[1], padding: '2px 10px',
            borderRadius: '10px', fontSize: '12px', whiteSpace: 'nowrap'
        }).prop('outerHTML');
    }

    $('#stock-table').on('click', 'tbody tr', function(){
        const itemCode = $(this).data('product-code');
        if (itemCode) openLedger(String(itemCode));
    });

    function openLedger(itemCode){
        const row = stockRows.find(r => r.item_CD === itemCode);
        const tenSP = row ? row.item_name : '';

        $('#ledger-overlay').remove();

        const overlay = $('<div>').attr('id', 'ledger-overlay').css({
            position:'fixed', inset:0, background:'rgba(0,0,0,0.45)', display:'flex',
            alignItems:'center', justifyContent:'center', zIndex:3000
        });
        const box = $('<div>').css({
            background:'#fff', borderRadius:'6px', padding:'24px', width:'900px',
            maxWidth:'94vw', maxHeight:'86vh', overflowY:'auto',
            boxShadow:'0 8px 24px rgba(0,0,0,0.2)'
        });

        box.append($('<h3>').text('Sổ cái kho: ' + itemCode + (tenSP ? ' - ' + tenSP : ''))
                            .css({marginBottom:'6px'}));
        box.append($('<p>').text('Quy ước dấu: nhập kho ghi số dương, xuất kho ghi số âm, ' +
                                 'điều chỉnh ghi phần chênh lệch. Mỗi dòng luôn thoả ' +
                                 'Tồn trước + Số lượng = Tồn sau. ' +
                                 'Ghi sai thì KHÔNG sửa dòng cũ - bấm "Đảo" để ghi thêm một ' +
                                 'bút toán ngược dấu, cả hai dòng đều nằm lại trong sổ.')
                           .css({color:'#7f8c8d', fontSize:'12px', lineHeight:'1.5', marginBottom:'14px'}));

        const body = $('<div>').text('Đang tải...').css({fontSize:'14px', color:'#7f8c8d'});
        const btnClose = $('<button>').text('Đóng').css({
            background:'#7f8c8d', color:'#fff', border:'none', borderRadius:'4px',
            padding:'8px 16px', cursor:'pointer', marginTop:'14px'
        }).on('click', function(){ overlay.remove(); });

        box.append(body).append(btnClose);
        overlay.append(box).appendTo('body');
        overlay.on('click', function(e){ if (e.target === this) overlay.remove(); });

        fetchAllPages('/api/stock/' + encodeURIComponent(itemCode) + '/transactions', {})
            .then(function(items){
                if (!items.length) {
                    body.text('Sản phẩm này chưa phát sinh biến động kho nào.');
                    return;
                }

                const table = $('<table>').css({width:'100%', borderCollapse:'collapse', fontSize:'13px'});
                const th = 'padding:8px;text-align:left;border-bottom:2px solid #ddd;background:#f5f5f5;white-space:nowrap';
                table.append($('<thead>').append(
                    '<tr>' +
                    '<th style="' + th + '">Thời điểm</th>' +
                    '<th style="' + th + '">Loại</th>' +
                    '<th style="' + th + '">Số lượng</th>' +
                    '<th style="' + th + '">Tồn trước</th>' +
                    '<th style="' + th + '">Tồn sau</th>' +
                    '<th style="' + th + '">Chứng từ</th>' +
                    '<th style="' + th + '">Người thực hiện</th>' +
                    '<th style="' + th + '">Ghi chú</th>' +
                    (canManageStock() ? '<th style="' + th + '"></th>' : '') +
                    '</tr>'));

                const tbody = $('<tbody>');
                const td = 'padding:8px;border-bottom:1px solid #eee;vertical-align:top';

                items.forEach(function(t){
                    let chungTu;
                    if (t.refType === 'ORDER') {
                        chungTu = t.refNo ? 'Đơn ' + t.refNo : 'Đơn đã xoá';
                    } else if (t.refType === 'REVERSAL') {
                        chungTu = 'Đảo #' + t.originalTransactionId;
                    } else {
                        chungTu = 'Thủ công';
                    }

                    const daBiDao = items.some(x => x.originalTransactionId === t.id);
                    const daoDuoc = canManageStock() &&
                                    t.transactionType !== 'REVERSAL' &&
                                    !daBiDao;

                    const oThaoTac = canManageStock()
                        ? '<td style="' + td + ';white-space:nowrap">' +
                          (daoDuoc
                            ? '<button class="btn-reverse" data-id="' + t.id + '" ' +
                              'style="padding:3px 8px;font-size:12px;cursor:pointer">Đảo</button>'
                            : (daBiDao ? '<span style="color:#95a5a6;font-size:12px">đã đảo</span>' : '')) +
                          '</td>'
                        : '';

                    tbody.append($('<tr>').html(
                        '<td style="' + td + ';white-space:nowrap">' + escapeHtml(formatDateTime(t.transactionDate)) + '</td>' +
                        '<td style="' + td + '">' + typeBadge(t.transactionType, t.transactionTypeText) + '</td>' +
                        '<td style="' + td + ';text-align:right;white-space:nowrap;color:' +
                            (t.quantity < 0 ? '#c0392b' : '#1e8449') + '">' +
                            (t.quantity > 0 ? '+' : '') + escapeHtml(formatAmount(t.quantity)) + '</td>' +
                        '<td style="' + td + ';text-align:right;white-space:nowrap;color:#7f8c8d">' + escapeHtml(formatAmount(t.balanceBefore)) + '</td>' +
                        '<td style="' + td + ';text-align:right;white-space:nowrap">' + escapeHtml(formatAmount(t.balanceAfter)) + '</td>' +
                        '<td style="' + td + ';white-space:nowrap">' + escapeHtml(chungTu) + '</td>' +
                        '<td style="' + td + '">' + escapeHtml(t.createdByName) + '</td>' +
                        '<td style="' + td + '">' + escapeHtml(t.note || '') + '</td>' +
                        oThaoTac));
                });

                body.empty()
                    .append($('<div>').css({overflowX:'auto'}).append(table.append(tbody)));

                body.off('click', '.btn-reverse').on('click', '.btn-reverse', function(){
                    reverseTransaction($(this).data('id'), itemCode);
                });
            })
            .catch(function(err){
                body.text(err.message || 'Không tải được sổ cái.').css({color:'#c0392b'});
            });
    }

    function reverseTransaction(transactionId, itemCode) {
        const reason = prompt(
            'Đảo bút toán #' + transactionId + '?\n\n' +
            'Bút toán gốc được GIỮ NGUYÊN, hệ thống ghi thêm một dòng ngược dấu để ' +
            'huỷ hiệu lực của nó. Cả hai dòng đều nằm lại trong sổ cái.\n\n' +
            'Nhập lý do (tối thiểu 5 ký tự):');

        if (reason === null) return;

        if (reason.trim().length < 5) {
            alert('Lý do phải có ít nhất 5 ký tự.');
            return;
        }

        fetch('/api/stock/reverse', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ transactionId: transactionId, reason: reason.trim() })
        })
        .then(res => res.json())
        .then(function(result){
            if (!result.success) {
                alert(result.message || 'Không đảo được bút toán.');
                return;
            }
            alert(result.message);
            fetchStock($("#search-input").val().trim()).then(function(){
                openLedger(itemCode);
            });
        })
        .catch(function(){
            alert('Lỗi kết nối server.');
        });
    }

});
