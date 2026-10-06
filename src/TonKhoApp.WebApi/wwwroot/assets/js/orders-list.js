$(function(){
    let table;
    function render(data) {
        if ($.fn.DataTable.isDataTable('#PO-table')) {
            $('#PO-table').DataTable().clear().destroy();
        }
        table = $('#PO-table').DataTable({
            data: data,
            columns: [
                { data: null, title: 'STT' },
                { data: 'orderNo', title: 'Mã đơn hàng', render: renderText },
                { data: 'vendorName', title: 'Tên NCC', render: renderText },
                { data: 'orderDate', title: 'Ngày mua hàng', render: renderText },
                { data: 'handlerName', title: 'Nhân viên phụ trách', render: renderText },
                { data: 'totalAmount', title: 'Tổng tiền', render: function(data){ return formatAmount(data); } },
                { data: 'statusText', title: 'Trạng thái', render: renderText }
            ],
            columnDefs: [
                {
                    targets: 0,
                    render: function (data, type, row, meta) {
                        return meta.row + 1;
                    }
                },
            ],
            createdRow: function (row, rowData, dataIndex) {
                $(row).attr('data-order-no', rowData.orderNo)
            },
            searching: false,
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

    function fetchOrders(keyword = "") {
        fetchAllPages('/api/orders', keyword ? { search: keyword } : {})
            .then(render)
            .catch(err => {
                render([]);
                alert(err.message || 'Không tải được danh sách đơn hàng.');
            });
    }

    fetchOrders();

    $("#btn-create").toggle(canEditOrder()).on("click", function () {
        window.location.href = "/order-detail?mode=new";
    });

    $("#btn-search").on("click", function(){
        let keyword = $("#search-input").val().trim();
        fetchOrders(keyword);
    });

    $("#search-input").on("keypress", function (event) {
        if (event.which === 13) $("#btn-search").click();
    });

    $('#PO-table').on('click', 'tbody tr', function () {
        let orderNo = $(this).data('order-no');
        window.location.href = `/order-detail?orderNo=${orderNo}`;
    });

});
