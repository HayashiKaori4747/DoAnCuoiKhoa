$(function(){
    let table;

    function statusBadge(isActive){
        return isActive === false
            ? '<span style="background:#e5e7e9;color:#5d6d7e;padding:2px 10px;border-radius:10px;font-size:12px;white-space:nowrap">Ngừng HT</span>'
            : '<span style="background:#d5f5e3;color:#1e8449;padding:2px 10px;border-radius:10px;font-size:12px;white-space:nowrap">Đang hợp tác</span>';
    }

    function renderVendors(data) {
        if ($.fn.DataTable.isDataTable('#vendors-table')) {
            $('#vendors-table').DataTable().clear().destroy();
        }
        table = $('#vendors-table').DataTable({
            data: data,
            columns: [
                { data: null, title: 'STT', render: function(data,type,row,meta){ return meta.row + 1; } },
                { data: 'vendorCode', title: 'Mã NCC', render: renderText },
                { data: 'vendorName', title: 'Tên nhà cung cấp', render: renderText },
                { data: 'address', title: 'Địa chỉ', render: renderText },
                { data: 'phone', title: 'Điện thoại', render: renderText },
                { data: 'isActive', title: 'Trạng thái', render: function(data){ return statusBadge(data); } }
            ],
            createdRow: function(row, rowData) {
                $(row).attr('data-vendor-id', rowData.id);
                if (rowData.isActive === false) $(row).css('opacity', '0.55');
            },
            searching: false,
            language: {
                emptyTable: "Không có dữ liệu",
                info: "Hiển thị _START_ đến _END_ của _TOTAL_ dòng",
                infoEmpty: "Hiển thị 0 đến 0 của 0 dòng",
                lengthMenu: "Hiển thị _MENU_ dòng",
                loadingRecords: "Đang tải...",
                zeroRecords: "Không tìm thấy dữ liệu phù hợp",
                paginate: { first:"Đầu tiên", last:"Cuối cùng", next:"Sau", previous:"Trước" }
            }
        });
    }

    function fetchVendors(keyword) {
        fetchAllPages('/api/vendors', keyword ? { search: keyword } : {})
            .then(renderVendors)
            .catch(err => {
                renderVendors([]);
                alert(err.message || 'Không tải được danh sách nhà cung cấp.');
            });
    }

    fetchVendors();

    $("#btn-search").on("click", function(){
        let keyword = $("#search-input").val().trim();
        fetchVendors(keyword);
    });

    $("#search-input").on("keypress", function(e){
        if(e.which === 13) $("#btn-search").click();
    });

    $('#vendors-table').on('click', 'tbody tr', function(){
        let vendorId = $(this).data('vendor-id');
        if (vendorId) {
            window.location.href = '/vendor-detail?vendorId=' + vendorId;
        }
    });

    $("#btn-create").toggle(canManageCatalog()).on("click", function(){
        window.location.href = "/vendor-detail?mode=new";
    });

});
