namespace MyCourtBooking.Models.ViewModels
{
    public class CourtReportViewModel
    {
        public string CourtName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public int TotalBookings { get; set; }
        public int OccupancyRate { get; set; }
        public decimal Revenue { get; set; }
    }
}