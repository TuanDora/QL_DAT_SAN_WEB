// Script dùng chung toàn hệ thống
document.addEventListener("DOMContentLoaded", function () {
    // Tự động ẩn thông báo Alert thành công / lỗi sau 4 giây
    setTimeout(function () {
        $(".alert-dismissible").fadeOut("slow", function () {
            $(this).remove();
        });
    }, 4000);
});

// Hàm xác nhận thao tác nguy hiểm (Xóa, Hủy đơn)
function confirmAction(message) {
    return confirm(message || "Bạn có chắc chắn muốn thực hiện thao tác này?");
}