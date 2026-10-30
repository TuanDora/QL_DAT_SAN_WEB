using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyCourtBooking.Core.Entities
{
    public class BookingDetail
    {
        [Key]
        public int Id { get; set; }

        public int BookingId { get; set; }
        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }
        // Bổ sung ngày chơi (Sửa lỗi dòng 242 trong AccountController)
        [DataType(DataType.Date)]
        public DateTime PlayDate { get; set; } = DateTime.Now;
        public int CourtId { get; set; }
        [ForeignKey("CourtId")]
        public virtual Court? Court { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }
    }
}