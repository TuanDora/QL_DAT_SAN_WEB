namespace MyCourtBooking.Models.Enums
{
    public enum BookingStatus
    {
        Pending = 0,    // Chờ duyệt / Chờ xác nhận thanh toán
        Confirmed = 1,  // Đã xác nhận thành công
        Cancelled = 2,  // Đã hủy đơn
        Completed = 3   // Đã hoàn thành (sau khi chơi xong)
    }
}