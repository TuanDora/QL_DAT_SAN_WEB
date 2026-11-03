namespace MyCourtBooking.Core.Enums
{
    public enum BookingStatus
    {
        Pending = 0,    // Chờ duyệt / Chờ xác nhận thanh toán (Khóa slot tạm thời 10p)
        Confirmed = 1,  // Đã xác nhận thành công / Đã thanh toán
        Cancelled = 2,  // Đã hủy đơn
        Completed = 3,  // Đã hoàn thành (sau khi chơi xong)
        Expired = 4     // Quá hạn 10 phút chưa thanh toán (Tự động giải phóng slot)
    }
}