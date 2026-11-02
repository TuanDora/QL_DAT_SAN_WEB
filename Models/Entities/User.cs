using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyCourtBooking.Models.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(20)]
        public string? Role { get; set; } = "Customer";

        public int? RoleId { get; set; }
        // Thêm thuộc tính Avatar mà AccountController đang gọi
        public string? Avatar { get; set; } = "/images/avatars/default.png";

        public  bool? IsActive { get; set; } = true;

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}