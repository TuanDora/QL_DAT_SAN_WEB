using System;
using System.Collections.Generic;

namespace MyCourtBooking.Models.DTOs
{
    public class SelectSlotViewModel
    {
        public int CourtId { get; set; }
        public string CourtName { get; set; } = string.Empty;
        public string CourtAddress { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public DateTime SelectedDate { get; set; }
        public List<SlotViewModel> Slots { get; set; } = new List<SlotViewModel>();
    }

    public class SlotViewModel
    {
        public int CourtId { get; set; }
        public string CourtName { get; set; } = string.Empty;
        public int SlotIndex { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string Status { get; set; } = "empty"; // "empty", "booked", "locked"
        public decimal PricePerHour { get; set; }
        public decimal PricePerSlot { get; set; }
        public string PriceLabel { get; set; } = string.Empty;
    }

    

    public class BookingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string BookingCode { get; set; } = string.Empty;
        public decimal DurationHours { get; set; }
        public decimal TotalPrice { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;

        public static BookingResult Fail(string message)
        {
            return new BookingResult
            {
                Success = false,
                Message = message
            };
        }
    }
}