using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyCourtBooking.Core.Entities
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Tiền mặt";
        public string? Status { get; set; } = "Completed";
        [StringLength(100)]
        public string? TransactionId { get; set; }

        public DateTime PaidAt { get; set; } = DateTime.Now;

        public bool IsSuccess { get; set; } = true;
    }
}