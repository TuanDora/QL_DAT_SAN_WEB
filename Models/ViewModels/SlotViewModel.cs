namespace MyCourtBooking.Models.ViewModels
{
    public  class SlotViewModel
    {
        public int CourtId { get; set; }

        public string CourtName { get; set; } = string.Empty;

        public int SlotIndex { get; set; }

        public string StartTime { get; set; } = string.Empty;

        public string EndTime { get; set; } = string.Empty;

        public string Status { get; set; } = "empty";

        public decimal PricePerHour { get; set; }

        public decimal PricePerSlot { get; set; }

        public string PriceLabel { get; set; } = string.Empty;

        public string StatusText => Status switch
        {
            "booked" => "Đã đặt",
            "locked" => "Đã khóa",
            _ => "Trống"
        };
    }
}