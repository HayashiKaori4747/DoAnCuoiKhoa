$(function(){
    let table;

    function statusBadge(isActive){
        return isActive === false
            ? '<span style="background:#e5e7e9;color:#5d6d7e;padding:2px 10px;border-radius:10px;font-size:12px;white-space:nowrap">Ngừng KD</span>'
            : '<span style="background:#d5f5e3;color:#1e8449;padding:2px 10px;border-radius:10px;font-size:12px;white-space:nowrap">Đang bán</span>';
    }

    function renderProducts(data){
        if ($.fn.DataTable.isDataTable('#products-table')) {
            $('#products-table').DataTable().clear().destroy();
        }
        table = $('#products-table').DataTable({
            data: data,
            columns: [
                { data: null, title: 'STT', render: function(data,type,row,meta){ return meta.row + 1; } },
                { data: 'itemCode', title: 'Mã SP', render: renderText },
                { data: 'itemName', title: 'Tên sản phẩm', render: renderText },
                { data: 'unit', title: 'Đơn vị', render: renderText },
                { data: 'price', title: 'Đơn giá', render: function(data){ return formatAmount(data); } },
                { data: 'isActive', title: 'Trạng thái', render: function(data){ return statusBadge(data); } }
            ],
            createdRow: function(row, rowData){
                $(row).attr('data-product-code', rowData.itemCode);
                if (rowData.isActive === false) $(row).css('opacity', '0.55');
            },
            searching: false,
            language: {
                emptyTable: "Không có dữ liệu trong bảng",
                info: "Hiển thị _START_ đến _END_ của _TOTAL_ dòng",
                infoEmpty: "Hiển thị 0 đến 0 của 0 dòng",
                lengthMenu: "Hiển thị _MENU_ dòng",
                loadingRecords: "Đang tải...",
                zeroRecords: "Không tìm thấy dữ liệu phù hợp",
                paginate: { first:"Đầu tiên", last:"Cuối cùng", next:"Sau", previous:"Trước" }
            }
        });
    }

    function fetchProducts(keyword){
        fetchAllPages('/api/products', keyword ? { search: keyword } : {})
            .then(renderProducts)
            .catch(err => {
                renderProducts([]);
                alert(err.message || 'Không tải được danh sách sản phẩm.');
            });
    }

    fetchProducts();

    $("#btn-search").on("click", function(){
        let keyword = $("#search-input").val().trim();
        fetchProducts(keyword);
    });

    $("#search-input").on("keypress", function(e){
        if(e.which === 13) $("#btn-search").click();
    });

    $('#products-table').on('click', 'tbody tr', function(){
        let productCode = $(this).data('product-code');
        if (productCode) {
            window.location.href = '/product-detail?productCode=' + encodeURIComponent(productCode);
        }
    });

    $("#btn-create").toggle(canManageCatalog()).on("click", function(){
        window.location.href = "/product-detail?mode=new";
    });

});
