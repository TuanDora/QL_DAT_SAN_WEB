using System;
using System.Collections.Generic;

namespace MyCourtBooking.Models.ViewModels
{
    public  class CreateBookingRequest
    {
        public int CourtId { get; set; }

        public DateTime Date { get; set; }

        public List<string> StartTimes { get; set; } = new List<string>();

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerPhone { get; set; } = string.Empty;

        public string? Note { get; set; }

        public string? PaymentMethod { get; set; } = "Tiền mặt";
    }
}