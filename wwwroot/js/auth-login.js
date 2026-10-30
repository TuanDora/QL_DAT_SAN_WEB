document.addEventListener("DOMContentLoaded", function () {
    var $ = window.jQuery;
    if ($) {
        $('#btnSendOtp').click(function () {
            var phone = $('#txtPhoneNumber').val();
            if (!phone) {
                alert('Vui lòng nhập số điện thoại!');
                return;
            }

            var token = $('input[name="__RequestVerificationToken"]').val();

            $.ajax({
                url: '/Auth/SendOtp',
                type: 'POST',
                data: {
                    phoneNumber: phone,
                    __RequestVerificationToken: token
                },
                success: function (res) {
                    if (res && res.success) {
                        $('#otpSection').removeClass('d-none');
                        $('#btnSendOtp').addClass('d-none');
                        $('#btnVerifyOtp').removeClass('d-none');
                        alert('Mã OTP thử nghiệm đã được gửi: ' + res.demoOtp);
                    } else {
                        alert(res ? res.message : 'Có lỗi xảy ra');
                    }
                }
            });
        });
    }
});