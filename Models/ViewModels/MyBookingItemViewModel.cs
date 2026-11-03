using System;
using System.Collections.Generic;

namespace MyCourtBooking.Models.ViewModels
{
    public class MyBookingItemViewModel
    {
        public int Id { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string CustomerPhone { get; set; } = string.Empty;
        public List<MyBookingDetailItem> Details { get; set; } = new List<MyBookingDetailItem>();
    }

    public class MyBookingDetailItem
    {
        public string CourtName { get; set; } = string.Empty;
        public DateTime PlayDate { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }
}