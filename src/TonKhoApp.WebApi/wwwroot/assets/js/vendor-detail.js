$(function(){
    const urlParams = new URLSearchParams(window.location.search);
    const vendorId = urlParams.get("vendorId");
    const mode = urlParams.get("mode");
    let isEdit = false;

    const FIELDS = {
        VendorCode: '#vendor-code',
        VendorName: '#vendor-name',
        Address:    '#vendor-address',
        Phone:      '#vendor-phone',
        Email:      '#vendor-email',
        TaxCode:    '#vendor-taxcode'
    };
    bindFieldErrorReset('.container');

    const readOnly = !canManageCatalog();
    if (readOnly && mode === "new") {
        alert('Vai trò của bạn không thêm được nhà cung cấp.');
        window.location.href = '/vendors-list';
        return;
    }
    if (readOnly) {
        $('.container input').prop('disabled', true);
        $('#saveBtn').hide();
        $('#backBtn').text('Quay lại');
    }

    if (vendorId && mode !== "new") {
        isEdit = true;
        fetch('/api/vendors/' + encodeURIComponent(vendorId))
            .then(res => res.json())
            .then(result => {
                if (result.success) {
                    $("#vendor-id").val(result.data.id);
                    $("#vendor-code").val(result.data.vendorCode);
                    $("#vendor-name").val(result.data.vendorName);
                    $("#vendor-address").val(result.data.address);
                    $("#vendor-phone").val(result.data.phone);
                    $("#vendor-email").val(result.data.email);
                    $("#vendor-taxcode").val(result.data.taxCode);
                    $("#is-active").prop("checked", result.data.isActive !== false);
                    $("#vendor-code").prop("disabled", true);
                }
            });
    }

    $("#saveBtn").on("click", async function(){
        clearFieldErrors('.container');

        const vendorCode = $("#vendor-code").val().trim();
        const vendorName = $("#vendor-name").val().trim();
        const address = $("#vendor-address").val().trim();
        const phone = $("#vendor-phone").val().trim();
        const email = $("#vendor-email").val().trim();
        const taxCode = $("#vendor-taxcode").val().trim();
        const isActive = $("#is-active").is(":checked");

        if(!vendorName){
            setFieldError('#vendor-name', 'Vui lòng nhập tên nhà cung cấp');
            $('#vendor-name').trigger('focus');
            return;
        }

        if (isEdit && !isActive) {
            const ok = confirm(
                "Đặt nhà cung cấp này sang NGỪNG HỢP TÁC?\n\n" +
                "Các đơn hàng cũ giữ nguyên, nhưng sẽ không chọn được NCC này khi lập đơn mới.\n\n" +
                "Tích lại ô đó bất cứ lúc nào để mở lại.");
            if (!ok) return;
        }

        const optional = {
            address: address || null,
            phone:   phone   || null,
            email:   email   || null,
            taxCode: taxCode || null
        };

        let url, method, body;
        if (isEdit) {
            url = '/api/vendors/' + encodeURIComponent(vendorId);
            method = 'PUT';
            body = Object.assign({ vendorName, isActive }, optional);
        } else {
            url = '/api/vendors';
            method = 'POST';
            body = Object.assign({ vendorCode: vendorCode || null, vendorName, isActive }, optional);
        }

        const res = await fetch(url, {
            method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });
        const result = await res.json();

        if (result.success) {
            alert(result.message || 'Lưu nhà cung cấp thành công');
            window.location.href = "/vendors-list";
        } else if (!showFieldErrors(result, FIELDS)) {
            alert(result.message || 'Lưu nhà cung cấp không thành công.');
        }
    });

    $("#backBtn").on("click", function(){
        window.location.href = "/vendors-list";
    });

});
