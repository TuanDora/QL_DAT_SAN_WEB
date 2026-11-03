namespace MyCourtBooking.Models.ViewModels
{
    public class BookingHistoryItemViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public decimal FinalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;

        public List<BookingDetailItem> Details { get; set; } = new List<BookingDetailItem>();

        // Kiểm tra xem đơn đặt sân có đủ điều kiện để hủy hay không
        // Điều kiện: Đơn ở trạng thái Pending hoặc Confirmed VÀ Ngày chơi gần nhất cách ít nhất 24 giờ
        public bool CanCancel
        {
            get; set;
        }
    }

    public class BookingDetailItem
    {
        public string CourtName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime PlayDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal Price { get; set; }

        public string TimeSlotString => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    }
}