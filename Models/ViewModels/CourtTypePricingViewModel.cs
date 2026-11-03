namespace MyCourtBooking.Models.ViewModels
{
    public class CourtTypePricingViewModel
    {
        public int TypeId { get; set; }
        public string Type { get; set; } = string.Empty;
        public int CourtCount { get; set; }
        public decimal PricePerHour { get; set; }
        public decimal PeakPricePerHour { get; set; }
    }
}