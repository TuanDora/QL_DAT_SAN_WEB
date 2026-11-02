using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyCourtBooking.Core.Entities;

namespace MyCourtBooking.Models.Entities
{
    // Bảng giá khung giờ chơi
    public class TimeSlotPrice
    {
        [Key]
        public int Id { get; set; }

        public int CourtId { get; set; }
        [ForeignKey("CourtId")]
        public virtual Court? Court { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public bool IsPeakHour { get; set; } = false; // Giờ cao điểm (thường từ 17h-21h)
    }
}