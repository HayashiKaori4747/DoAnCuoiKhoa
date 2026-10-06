$(function(){
    const urlParams = new URLSearchParams(window.location.search);
    const productCode = urlParams.get("productCode");
    const mode = urlParams.get("mode");
    let isEdit = false;

    const FIELDS = {
        ItemCode: '#item_CD',
        ItemName: '#item_name',
        Unit:     '#unit',
        Price:       '#price',
        MinQuantity: '#min-quantity'
    };
    bindFieldErrorReset('.container');

    const readOnly = !canManageCatalog();
    if (readOnly && mode === "new") {
        alert('Vai trò của bạn không thêm được sản phẩm.');
        window.location.href = '/products-list';
        return;
    }
    if (readOnly) {
        $('.container input').prop('disabled', true);
        $('#saveBtn').hide();
        $('#backBtn').text('Quay lại');
    }

    if (productCode && mode !== "new") {
        isEdit = true;
        fetch('/api/products/' + encodeURIComponent(productCode))
            .then(res => res.json())
            .then(result => {
                if (result.success) {
                    $("#item_CD").val(result.data.itemCode);
                    $("#item_name").val(result.data.itemName);
                    $("#unit").val(result.data.unit);
                    $("#price").val(result.data.price);
                    $("#min-quantity").val(result.data.minQuantity);
                    $("#is-active").prop("checked", result.data.isActive !== false);
                    $("#item_CD").prop("disabled", true);
                }
            });
    }

    $("#saveBtn").on("click", async function(){
        clearFieldErrors('.container');

        const itemCode = $("#item_CD").val().trim();
        const itemName = $("#item_name").val().trim();
        const unit = $("#unit").val().trim();
        const priceRaw = $("#price").val().trim();
        const price = parseFloat(priceRaw);

        const minRaw = $("#min-quantity").val().trim();
        const minQuantity = minRaw === '' ? 0 : parseFloat(minRaw);

        const isActive = $("#is-active").is(":checked");

        let ok = true;
        if (!isEdit && !itemCode) { setFieldError('#item_CD', 'Vui lòng nhập mã sản phẩm'); ok = false; }
        if (!itemName) { setFieldError('#item_name', 'Vui lòng nhập tên sản phẩm'); ok = false; }
        if (!unit)     { setFieldError('#unit', 'Vui lòng nhập đơn vị tính'); ok = false; }

        if (priceRaw === '' || isNaN(price)) {
            setFieldError('#price', 'Vui lòng nhập đơn giá'); ok = false;
        } else if (price < 0) {
            setFieldError('#price', 'Đơn giá không được âm'); ok = false;
        }

        if (isNaN(minQuantity)) {
            setFieldError('#min-quantity', 'Định mức tồn phải là một số'); ok = false;
        } else if (minQuantity < 0) {
            setFieldError('#min-quantity', 'Định mức tồn không được âm'); ok = false;
        }

        if (!ok) { $('.field-invalid').first().trigger('focus'); return; }

        if (isEdit && !isActive) {
            const ok = confirm(
                "Đặt sản phẩm này sang NGỪNG KINH DOANH?\n\n" +
                "Đơn hàng và sổ kho cũ giữ nguyên, nhưng sẽ không chọn được sản phẩm này " +
                "khi lập đơn mới.\n\nTích lại ô đó bất cứ lúc nào để mở lại.");
            if (!ok) return;
        }

        let url, method, body;
        if (isEdit) {
            url = '/api/products/' + encodeURIComponent(productCode);
            method = 'PUT';
            body = { itemName, unit, price, minQuantity, isActive };
        } else {
            url = '/api/products';
            method = 'POST';
            body = { itemCode, itemName, unit, price, minQuantity, isActive };
        }

        const res = await fetch(url, {
            method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });
        const result = await res.json();

        if (result.success) {
            alert(result.message || 'Lưu sản phẩm thành công');
            window.location.href = "/products-list";
        } else if (!showFieldErrors(result, FIELDS)) {
            alert(result.message || 'Lưu sản phẩm không thành công.');
        }
    });

    $("#backBtn").on("click", function(){
        window.location.href = "/products-list";
    });

});
