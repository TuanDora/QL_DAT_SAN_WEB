using MyCourtBooking.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace MyCourtBooking.Models.Entities
{
    // Bảng Vai trò người dùng (Admin, CourtOwner, Customer)
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}