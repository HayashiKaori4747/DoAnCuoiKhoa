$(function(){
    const FIELDS = {
        UserId:       '#user-id',
        UserPassword: '#user-password'
    };

    function showError(msg) {
        $('#error-msg').text(msg).show();
    }
    function clearError() {
        $('#error-msg').hide().text('');
        clearFieldErrors('.login');
    }

    bindFieldErrorReset('.login');

    const returnUrl = new URLSearchParams(window.location.search).get('returnUrl');

    if (returnUrl) {
        $('.login').css('visibility', 'hidden');

        fetch('/api/auth/refresh', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: '{}'
        })
        .then(function(res){
            if (!res.ok) throw new Error('expired');

            const safe = /^\/[^\/]/.test(returnUrl) ? returnUrl : landingPage();
            window.location.replace(safe);
        })
        .catch(function(){
            $('.login').css('visibility', 'visible');
        });
    }

    function landingPage() {
        const raw = document.cookie.split('; ').find(r => r.startsWith('user_info='));
        if (!raw) return '/dashboard';
        try {
            const info = JSON.parse(decodeURIComponent(raw.split('=')[1]));
            const roles = (info.roles || []).map(r => String(r).toLowerCase());

            return roles.length === 1 && roles[0] === 'warehouse'
                ? '/orders-list'
                : '/dashboard';
        } catch (e) {
            return '/dashboard';
        }
    }

    $(".login").on("submit", async function(event){
        event.preventDefault();
        clearError();

        const userId = $("#user-id").val().trim();

        const userPassword = $("#user-password").val();

        let ok = true;
        if (!userId)       { setFieldError('#user-id', 'Vui lòng nhập User ID'); ok = false; }
        if (!userPassword) { setFieldError('#user-password', 'Vui lòng nhập Password'); ok = false; }
        if (!ok) { $('.field-invalid').first().trigger('focus'); return; }

        try {
            const res = await fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ userId, userPassword })
            });

            const data = await res.json();

            if(data.success){
                window.location.href = (returnUrl && /^\/[^\/]/.test(returnUrl))
                    ? returnUrl
                    : landingPage();
            } else if (!showFieldErrors(data, FIELDS)) {
                showError(data.message || 'Đăng nhập không thành công.');
            }
        } catch(err) {
            console.error(err);
            showError('Lỗi kết nối, vui lòng thử lại.');
        }
    });
});
