// Script thao tác dành riêng cho trang Quản trị Admin
function updateBookingStatusViaAjax(bookingId, statusValue) {
    if (!confirm("Bạn có chắc chắn muốn cập nhật trạng thái đơn đặt sân này?")) return;

    fetch('/Admin/UpdateBookingStatus', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `bookingId=${bookingId}&status=${statusValue}`
    })
        .then(response => {
            if (response.ok) {
                window.location.reload();
            } else {
                alert("Có lỗi xảy ra khi cập nhật trạng thái đơn!");
            }
        })
        .catch(err => console.error(err));
}