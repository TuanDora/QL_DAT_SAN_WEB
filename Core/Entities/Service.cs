using System.ComponentModel.DataAnnotations.Schema;

namespace MyCourtBooking.Core.Entities
{
    [Table("Services")] // Ép Entity Framework ánh xạ chính xác vào bảng 'Services'
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; } = true;
    }
}