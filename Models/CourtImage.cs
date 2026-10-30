using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyCourtBooking.Core.Entities;

namespace MyCourtBooking.Core.Entities
{
    // Hình ảnh của sân
    public class CourtImage
    {
        [Key]
        public int Id { get; set; }

        public int CourtId { get; set; }
        [ForeignKey("CourtId")]
        public virtual Court? Court { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;
    }
}