function parseVN(str) {
    if (!str) return 0;
    return parseFloat(String(str).replace(/,/g, '')) || 0;
}

function fetchProductsForSearch(callback, keyword) {
    const params = { activeOnly: 'true' };
    if (keyword) params.search = keyword;

    fetchAllPages('/api/products', params)
        .then(callback)
        .catch(err => {
            callback([]);
            alert(err.message || 'Không tải được danh sách sản phẩm.');
        });
}

$(function() {
    const queryString = window.location.search;
    const urlParams = new URLSearchParams(queryString);
    const orderNo = urlParams.get("orderNo");
    let mode = urlParams.get("mode");

    if (mode === "new" && !canEditOrder()) {
        alert('Vai trò của bạn không lập được đơn mua hàng.');
        window.location.href = '/orders-list';
        return;
    }

    if (canEditOrder()) {
        $('#createNewBtn, #productSearch').show();
    }

    let order = null;
    let table;
    let searchTable;

    function render (data,editable){
        if ($.fn.DataTable.isDataTable('#PO-table')) {
            $('#PO-table').DataTable().clear().destroy();
        }

        table = $('#PO-table').DataTable({
            data: data,
            columns: [
                { data: null, title: 'STT', className: "dt-center", render: function (data, type, row, meta) { return meta.row + 1; } },
                { data: 'itemCode', title: 'Mã SP', className: "dt-center", render: function (data) { return editable ? `<input class='edit-input itemCode' value="${escapeHtml(data)}">` : escapeHtml(data); } },
                { data: 'itemName', title: 'Tên SP', className: "dt-center", render: function (data) { return editable ? `<input class='edit-input itemName' value="${escapeHtml(data)}">` : escapeHtml(data); } },
                { data: 'quantity', title: 'Số lượng', className: "dt-center", render: function (data) { return editable ? `<input type="number" class='edit-input quantity' value="${escapeHtml(data || '')}">` : escapeHtml(data); } },
                { data: 'unitPrice', title: 'Đơn giá', className: "dt-center", render: function (data) { return editable ? `<input type="text" class='edit-input unitPrice' value="${data ? formatAmount(data) : ''}">` : (data ? formatAmount(data) : ''); } },
                { data: null, title: 'Thành tiền', className: "dt-center", render: function (data, type, row) { const t = (row.lineAmount !== undefined && row.lineAmount !== null) ? row.lineAmount : lineAmount(row.quantity, row.unitPrice); return t > 0 ? formatAmount(t) : ''; } },
            ],
            searching: false,
            paging: false,
            language: {
                decimal: "",
                emptyTable: "Không có dữ liệu trong bảng",
                info: "Hiển thị _START_ đến _END_ của _TOTAL_ dòng",
                infoEmpty: "Hiển thị 0 đến 0 của 0 dòng",
                lengthMenu: "Hiển thị _MENU_ dòng",
                loadingRecords: "Đang tải...",
                zeroRecords: "Không tìm thấy dữ liệu phù hợp",
                paginate: {
                    first: "Đầu tiên",
                    last: "Cuối cùng",
                    next: "Sau",
                    previous: "Trước"
                }
            }
        });
        table.on('draw.dt', function () {
            table.column(0, { search: 'applied', order: 'applied' }).nodes().each(function (cell, i) { cell.innerHTML = i + 1 });
        });
    }

    $('#PO-table tbody').on('focus', '.unitPrice', function(){
        const raw = parseVN($(this).val());
        $(this).val(raw > 0 ? raw : '');
    });
    $('#PO-table tbody').on('blur', '.unitPrice', function(){
        const raw = parseVN($(this).val());
        $(this).val(raw > 0 ? formatAmount(raw) : '');
    });

    $('#PO-table tbody').on('keydown', 'input', function(e){
        if (e.key === 'Enter') {
            e.preventDefault();
            const currentRow = $(this).closest('tr');
            const currentColIndex = $(this).closest('td').index();
            const nextRow = currentRow.next('tr');
            if (nextRow.length) {
                const nextInput = nextRow.find('td:eq(' + currentColIndex + ') input');
                if (nextInput.length) {
                    nextInput.focus().select();
                }
            }
        }
    });

    let currentEditingRow = null;
    $('#PO-table tbody').on('click', 'tr', function(){
        $('#PO-table tbody tr').removeClass('selected-row');
        $(this).addClass('selected-row');
        currentEditingRow = $(this);
    });

    searchTable = $('#searchProductTable').DataTable({
        data: [],
        columns: [
            { data: 'itemCode', title: 'Mã SP', render: renderText },
            { data: 'itemName', title: 'Tên sản phẩm', render: renderText },
            { data: 'unit', title: 'Đơn vị', render: renderText },
            { data: 'price', title: 'Đơn giá', render: function (data) { return formatAmount(data); } }
        ],
        language: {
            decimal: "",
            emptyTable: "Không có dữ liệu trong bảng",
            info: "Hiển thị _START_ đến _END_ của _TOTAL_ dòng",
            infoEmpty: "Hiển thị 0 đến 0 của 0 dòng",
            lengthMenu: "Hiển thị _MENU_ dòng",
            loadingRecords: "Đang tải...",
            zeroRecords: "Không tìm thấy dữ liệu phù hợp",
            paginate: {
                first: "Đầu tiên",
                last: "Cuối cùng",
                next: "Sau",
                previous: "Trước"
            }
        },
        searching: false,
        info: false,
        paging: true
    });

    $('#productSearch').on('click', function () {
        if (!currentEditingRow) return;
        $('#searchProductInput').val("");

        fetchProductsForSearch(function (productsData) {
            searchTable.clear().rows.add(productsData).draw();
            $('.search-table').fadeIn();
        });
    });

    $('#btn-close-popup').on('click',function(){
        $('.search-table').fadeOut();
    });

    $('#btn-show-products').on('click', function () {
        let keyword = $('#searchProductInput').val().trim();
        fetchProductsForSearch(function (productsData) {
            searchTable.clear().rows.add(productsData).draw();
        }, keyword);
    });

    $('#searchProductTable').on('dblclick','tbody tr',function(){
        let data = searchTable.row(this).data();
        if(!currentEditingRow)return;
        currentEditingRow.find('.itemCode').val(data.itemCode);
        currentEditingRow.find('.itemName').val(data.itemName);
        currentEditingRow.find('.unitPrice').val(formatAmount(data.price));
        $('.search-table').fadeOut();
    })

    function calculateTotalAmount(displayProducts) {
        return displayProducts.reduce(function (sum, p) {
            const t = (p.lineAmount !== undefined && p.lineAmount !== null)
                ? Number(p.lineAmount)
                : lineAmount(p.quantity, p.unitPrice);
            return sum + t;
        }, 0);
    }

    function updateTotalAmount() {
        let total = 0;
        $('#PO-table tbody tr').each(function() {
            let quantity = parseFloat($(this).find('.quantity').val()) || 0;
            let price = parseVN($(this).find('.unitPrice').val());
            total += lineAmount(quantity, price);
        });
        $("input[name='net1']").val(formatAmount(total));
    }

    function updateOrderData (){
        let updatedProducts = [];
        $('#PO-table tbody tr').each(function () {
            const itemCode = $(this).find('.itemCode').val()?.trim();
            if (!itemCode) return;
            updatedProducts.push({
                itemCode,
                itemName: $(this).find('.itemName').val() || "",
                quantity: parseFloat($(this).find('.quantity').val()) || 0,
                unitPrice: parseVN($(this).find('.unitPrice').val())
            });
        });

        if(order){
            order.orderProducts = updatedProducts;
            order.totalAmount = parseFloat($("input[name='net1']").val().replace(/,/g, '')) || 0;
        }
    }

    function getOrderProductsFromTable() {
        const products = [];
        const loi = [];

        $('#PO-table tbody tr').each(function (index) {
            const $tr = $(this);
            const itemCode = ($tr.find('.itemCode').val() || '').trim();
            const itemName = ($tr.find('.itemName').val() || '').trim();
            const quantityRaw = ($tr.find('.quantity').val() || '').trim();
            const unitPrice = parseVN($tr.find('.unitPrice').val());

            if (!itemCode && !itemName && !quantityRaw && !unitPrice) {
                return;
            }

            if (!itemCode) {
                loi.push('Dòng ' + (index + 1) + ': đã nhập dữ liệu nhưng thiếu Mã SP');
                return;
            }

            products.push({
                itemCode: itemCode,
                quantity: parseFloat(quantityRaw) || 0,
                unitPrice: unitPrice
            });
        });

        return { products: products, loi: loi };
    }

    function layDongHangHopLe() {
        const ketQua = getOrderProductsFromTable();

        if (ketQua.loi.length > 0) {
            alert('Chưa lưu được, các dòng sau còn thiếu Mã SP:\n\n' + ketQua.loi.join('\n') +
                  '\n\nHãy bấm "Tìm kiếm SP" để chọn mã, hoặc xoá dòng đó đi.');
            return null;
        }

        if (ketQua.products.length === 0) {
            alert('Vui lòng nhập ít nhất 1 sản phẩm.');
            return null;
        }

        return ketQua.products;
    }

    function lockInputs() {
        $('#vendor, #PO-person, #PO-date, #addRow, #deleteRow, #saveBtn')
            .prop('disabled', true);

        $('#productSearch').prop('disabled', true).hide();

        $('#PO-table input, input[name="net1"], textarea[name="note1"]')
            .prop('readonly', true);
    }

    function unlockInputs() {
        $('#vendor, #PO-person, #PO-date, #addRow, #deleteRow, #saveBtn')
            .prop('disabled', false);

        $('#productSearch').prop('disabled', false).toggle(canEditOrder());

        $('#PO-table input, textarea[name="note1"]').prop('readonly', false);
    }

    function loadOrderDetail() {
        if (!orderNo) {

            $('#vendor').val('');
            $('#PO-person').val('');
            $('#PO-date').val('');
            $("input[name='net1']").val('');
            $("textarea[name='note1']").val('');
            const blankRows = [
                { itemCode: '', itemName: '', quantity: '', unitPrice: '' },
                { itemCode: '', itemName: '', quantity: '', unitPrice: '' },
                { itemCode: '', itemName: '', quantity: '', unitPrice: '' }
            ];
            render(blankRows, true);
            $('#addRow,#deleteRow,#saveBtn').show();
            $('#confirmOrderBtn,#confirmStockBtn').hide();
            return;
        }
        fetch(`/api/orders/${encodeURIComponent(orderNo)}`)
            .then(async res => ({ ok: res.ok, body: await res.json() }))
            .then(({ ok, body: result }) => {
                if (!ok || !result.success || !result.data) {
                    alert(result.message || `Không mở được đơn hàng ${orderNo}.`);
                    window.location.href = '/orders-list';
                    return;
                }
                order = result.data;
                selectOrAdd("#PO-person", order.handler, "(đã nghỉ)");
                if (order.orderDate) {
                    const parts = order.orderDate.split('-');
                    if (parts.length === 3) {
                        $("#PO-date").val(parts[2] + '/' + parts[1] + '/' + parts[0]);
                    } else {
                        $("#PO-date").val(order.orderDate);
                    }
                }
                selectOrAdd("#vendor", order.vendorName, "(ngừng hợp tác)");
                $("input[name='net1']").val(formatAmount(order.totalAmount));
                $("textarea[name='note1']").val(order.note || '');
                if (order.status === "DRAFT") {
                    const editable = canEditOrder();
                    render(order.orderProducts, editable);
                    const total = calculateTotalAmount(order.orderProducts);
                    $("input[name='net1']").val(formatAmount(total));
                    $('#addRow,#deleteRow,#saveBtn').toggle(editable);
                    if (!editable) lockInputs();
                    $('#confirmOrderBtn').toggle(canConfirmOrder());
                    $('#confirmStockBtn').hide();
                } else if (order.status === "CONFIRMED") {
                    render(order.orderProducts, false);
                    $('#addRow,#deleteRow,#saveBtn,#confirmOrderBtn').hide();
                    $('#confirmStockBtn').toggle(canManageStock());
                    lockInputs();
                } else {
                    render(order.orderProducts, false);
                    $('#addRow,#deleteRow,#saveBtn,#confirmOrderBtn,#confirmStockBtn').hide();
                    lockInputs();
                }

                renderCancelInfo();

                refreshDeleteButton();
            })
            .catch(function (err) {
                console.error(err);
                alert('Không mở được đơn hàng ' + orderNo + '.\n\n' +
                      (err && err.message ? err.message : 'Lỗi kết nối server.'));
                window.location.href = '/orders-list';
            });
    }

    function renderCancelInfo() {
        $('#cancel-banner').remove();
        if (!order || order.status !== 'CANCELLED') return;

        const parts = [];
        if (order.cancelledByName) parts.push('Người huỷ: ' + order.cancelledByName);
        if (order.cancelledAt)     parts.push('Lúc: ' + formatDateTime(order.cancelledAt));

        const banner = $('<div>').attr('id', 'cancel-banner').css({
            background: '#fadbd8', color: '#c0392b', border: '1px solid #e6b0aa',
            borderRadius: '4px', padding: '10px 12px', margin: '0 0 12px 0', fontSize: '14px'
        });

        banner.append($('<strong>').text('Đơn đã huỷ. '));
        banner.append(document.createTextNode('Lý do: ' + (order.cancelReason || '(không ghi)')));

        if (parts.length) {
            banner.append($('<div>').css({ marginTop: '4px', opacity: 0.85 }).text(parts.join('  •  ')));
        }

        banner.insertBefore($('#mainForm'));
    }

    function refreshDeleteButton() {
        const daLuu = !!(order && order.orderNo);
        const coQuyen = canEditOrder();

        $('#deleteOrderBtn').toggle(daLuu && coQuyen && order.status === 'DRAFT');
        $('#cancelOrderBtn').toggle(daLuu && coQuyen && order.status === 'CONFIRMED');
    }

    function selectOrAdd(selector, value, labelSuffix) {
        if (!value) return;
        const select = $(selector);
        const exists = select.find('option').filter(function () {
            return $(this).val() === value;
        }).length > 0;

        if (!exists) {
            select.append($('<option>').val(value).text(value + ' ' + labelSuffix));
        }
        select.val(value);
    }

    function loadVendors() {
        return fetchAllPages('/api/vendors', { activeOnly: 'true' })
            .then(function (vendors) {
                const select = $('#vendor');
                select.find('option:not(:first)').remove();
                vendors.forEach(function (v) {
                    select.append($('<option>').val(v.vendorName).text(v.vendorName));
                });
            })
            .catch(function (err) {
                alert(err.message || 'Không tải được danh sách nhà cung cấp.');
            });
    }

    function loadHandlers() {
        return fetch('/api/auth/users')
            .then(res => res.json())
            .then(result => {
                if (!result.success) {
                    throw new Error(result.message || 'Không tải được danh sách nhân viên.');
                }
                const select = $('#PO-person');
                select.find('option:not(:first)').remove();
                result.data.forEach(u => {
                    select.append($('<option>').val(u.userId).text(u.fullName));
                });
            })
            .catch(function (err) {
                alert(err.message || 'Không tải được danh sách nhân viên.');
            });
    }

    Promise.all([loadVendors(), loadHandlers()]).then(loadOrderDetail);

    $('#deleteOrderBtn').on('click', async function () {
        if (!order || !order.orderNo) return;

        if (!confirm(`Xoá bản nháp ${order.orderNo}?\n\n` +
                     'Đơn sẽ biến mất khỏi mọi màn hình. Chỉ dùng cho đơn lập nhầm - ' +
                     'nếu nhà cung cấp đã nhận đơn này thì hãy dùng "Huỷ đơn" kèm lý do.')) {
            return;
        }

        try {
            const res = await fetch(`/api/orders/${order.orderNo}`, { method: 'DELETE' });
            const result = await res.json();

            if (res.ok && result.success) {
                alert(result.message || 'Đã xoá đơn hàng');
                window.location.href = '/orders-list';
            } else {
                alert(result.message || 'Không xoá được đơn hàng.');
            }
        } catch (err) {
            alert('Lỗi kết nối server.');
        }
    });

    $('#cancelOrderBtn').on('click', async function () {
        if (!order || !order.orderNo) return;

        const reason = prompt(
            `Huỷ đơn ${order.orderNo}?\n\n` +
            'Đơn vẫn nằm trong danh sách với trạng thái "Đã huỷ" để còn thống kê được.\n' +
            'Nhập lý do huỷ (tối thiểu 5 ký tự):');

        if (reason === null) return;

        if (reason.trim().length < 5) {
            alert('Lý do huỷ phải có ít nhất 5 ký tự.');
            return;
        }

        try {
            const res = await fetch(`/api/orders/${encodeURIComponent(order.orderNo)}/cancel`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ reason: reason.trim() })
            });
            const result = await res.json();

            if (res.ok && result.success) {
                alert(result.message || 'Đã huỷ đơn hàng');
                window.location.href = '/orders-list';
            } else {
                alert(result.message || 'Không huỷ được đơn hàng.');
            }
        } catch (err) {
            alert('Lỗi kết nối server.');
        }
    });

    $('#createNewBtn').on('click', function() {
        unlockInputs();
        $('#vendor').val('');
        order = null;
        mode = "new";
        $('#deleteOrderBtn,#cancelOrderBtn').hide();
        $('#PO-person').val('');
        $('#PO-date').val('');
        $("input[name='net1']").val('');
        $("textarea[name='note1']").val('');

        const blankRows = [
        { itemCode: '', itemName: '', quantity: 0, unitPrice: 0 },
        { itemCode: '', itemName: '', quantity: 0, unitPrice: 0 },
        { itemCode: '', itemName: '', quantity: 0, unitPrice: 0 }
        ];

        render(blankRows,true);
        $('#addRow,#deleteRow,#saveBtn').show();
        $('#confirmOrderBtn,#confirmStockBtn').hide();
        const newUrl = window.location.pathname + "?mode=new";
        window.history.replaceState({}, '', newUrl);
    });

    if(mode === "new"){
        $('#vendor').val('');
        $('#PO-person').val('');
        $('#PO-date').val('');
        $("input[name='net1']").val('');
        $("textarea[name='note1']").val('');

        const blankRows = [
        { itemCode: '', itemName: '', quantity: 0, unitPrice: 0 },
        { itemCode: '', itemName: '', quantity: 0, unitPrice: 0 },
        { itemCode: '', itemName: '', quantity: 0, unitPrice: 0 }
        ];

        render(blankRows,true);
        $('#addRow,#deleteRow,#saveBtn').show();
        $('#confirmOrderBtn,#confirmStockBtn').hide();
    }

    $("#PO-table").on('input','.edit-input',function(){
        let $tr = $(this).closest('tr');
        let quantity = parseFloat($tr.find('.quantity').val()) || 0;
        let price = parseVN($tr.find('.unitPrice').val());

        let rowTotal = lineAmount(quantity, price);
        $tr.find('td').eq(5).text(rowTotal > 0 ? formatAmount(rowTotal) : '');

        updateTotalAmount();
        updateOrderData();
    });

    $('#addRow').on('click', function() {
        table.row.add({
            itemCode: '',
            itemName: '',
            quantity: 0,
            unitPrice: 0
        }).draw(false);
        updateTotalAmount();
        updateOrderData();
    });

    $('#deleteRow').on('click', function() {
        if (!currentEditingRow) {
            alert('Vui lòng click chọn dòng cần xóa.');
            return;
        }
        table.row(currentEditingRow).remove().draw();
        currentEditingRow = null;
        updateTotalAmount();
        updateOrderData();
    });

    $('#saveBtn').on('click', async function() {
        updateOrderData();

        const dongHang = layDongHangHopLe();
        if (!dongHang) return;

        let vendorName = $('#vendor').val().trim();
        let orderDate = $('#PO-date').val().trim();
        let handler = $('#PO-person').val().trim();

        if (!vendorName) {
            alert('Vui lòng chọn nhà cung cấp.');
            $('#vendor').focus();
            return;
        }
        if (!orderDate) {
            alert('Vui lòng nhập ngày đặt hàng.');
            $('#PO-date').focus();
            return;
        }
        if (!handler) {
            alert('Vui lòng nhập người phụ trách.');
            $('#PO-person').focus();
            return;
        }

        let orderDate_ = orderDate;
        const dateParts = orderDate.split('/');
        if (dateParts.length === 3) {
            orderDate_ = dateParts[2] + '-' + dateParts[1] + '-' + dateParts[0];
        }

        const orderData = {
            vendorName: vendorName,
            orderDate: orderDate_,
            handler: handler,
            totalAmount: parseFloat($("input[name='net1']").val().replace(/,/g, '')) || 0,
            note: $("textarea[name='note1']").val() || '',
            orderProducts: dongHang
        };

        try {
            let response;
            if (!order || mode === "new") {

                response = await fetch('/api/orders', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(orderData)
                });
            } else {

                response = await fetch(`/api/orders/${order.orderNo}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(orderData)
                });
            }

            if (!response.ok) {
                const text = await response.text();
                let msg = 'Lỗi khi lưu đơn hàng';
                try { msg = JSON.parse(text).message || msg; } catch(e) {}
                alert(msg);
                return;
            }
            const result = await response.json();
            alert(result.message);

            const daLaDonMoi = !order || mode === "new";
            order = Object.assign({}, order, result.data, {
                status: order && order.status ? order.status : "DRAFT"
            });

            if (daLaDonMoi) {
                mode = null;
                const newUrl = window.location.pathname + `?orderNo=${encodeURIComponent(order.orderNo)}`;
                window.history.replaceState({}, '', newUrl);
            }

            $('#confirmOrderBtn').toggle(canConfirmOrder());
            $('#confirmStockBtn').hide();
            refreshDeleteButton();
        } catch (err) {
            console.error(err);
            alert('Lỗi kết nối server: ' + err.message);
        }
    });

    $('#confirmOrderBtn').on('click', async function() {
        updateOrderData();

        if (!layDongHangHopLe()) return;

        if (!order) {
            alert('Vui lòng lưu đơn hàng trước khi xác nhận');
            return;
        }

        try {
            const response = await fetch(`/api/orders/${encodeURIComponent(order.orderNo)}/confirm`, {
                method: 'PUT'
            });

            const result = await response.json();
            if (response.ok && result.success) {
                alert(`Đơn hàng ${order.orderNo} đã được xác nhận.`);
                order.status = "CONFIRMED";
                refreshDeleteButton();
                lockInputs();
                $('#confirmOrderBtn, #saveBtn').hide();
                $('#confirmStockBtn').toggle(canManageStock());
            } else {
                alert(result.message || 'Lỗi khi xác nhận đơn hàng');
            }
        } catch (err) {
            alert('Lỗi kết nối server');
        }
    });

    $('#confirmStockBtn').on('click', async function () {
        if (!order || !order.orderNo) {
            alert('Đơn hàng chưa tải xong, vui lòng đợi một chút rồi thử lại.');
            return;
        }

        const products = table.rows().data().toArray();
        if (!products || products.length === 0) {
            alert("Không có sản phẩm để nhập kho");
            return;
        }
        try {
            const response = await fetch(`/api/orders/${encodeURIComponent(order.orderNo)}/stock`, {
                method: 'PUT'
            });
            const result = await response.json();
            if (response.ok && result.success) {
                alert(`Đơn hàng ${order.orderNo} đã được nhập kho`);
                $('#addRow,#deleteRow,#saveBtn,#confirmStockBtn,#confirmOrderBtn').hide();
                lockInputs();
                order.status = "RECEIVED";
                refreshDeleteButton();
            } else {
                alert(result.message || 'Lỗi khi nhập kho');
            }
        } catch (err) {
            alert('Lỗi kết nối server');
        }
    });

    $("#PO-date").datepicker({
        dateFormat: "dd/mm/yy"
    });

});
