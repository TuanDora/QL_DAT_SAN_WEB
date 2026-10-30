using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyCourtBooking.Core.Enums;

namespace MyCourtBooking.Core.Entities
{
    public class Court
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sân không được để trống")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        // Bổ sung Vị trí / Thành phố (Sửa lỗi dòng 148 trong Index.cshtml)
        [StringLength(100)]
        public string? City { get; set; } = "Hà Nội";

        // Bổ sung Đánh giá trung bình (Sửa lỗi dòng 249 CourtService & dòng 139 Index.cshtml)
        public double Rating { get; set; } = 5.0;

        // Bổ sung Danh sách hình ảnh sân (Sửa lỗi CourtImages ở các file)
        public virtual ICollection<CourtImage> CourtImages { get; set; } = new List<CourtImage>();

        // Bổ sung Danh sách đánh giá (Sửa lỗi Reviews ở các file)
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(255)]
        public string Location { get; set; } = string.Empty;

        // Các thuộc tính địa chỉ bổ sung cho AdminController
        [StringLength(100)]
        public string? District { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PricePerHour { get; set; }

        // Alias BasePrice tương thích với code cũ
        [NotMapped]
        public decimal BasePrice
        {
            get => PricePerHour;
            set => PricePerHour = value;
        }

        [Required]
        public CourtType CourtType { get; set; } = CourtType.Indoor;

        [NotMapped]
        public int CourtTypeId
        {
            get => (int)CourtType;
            set => CourtType = (CourtType)value;
        }

        [StringLength(255)]
        public string? ImageUrl { get; set; } = "/images/courts/default-court.jpg";

        public bool IsActive { get; set; } = true;

        public TimeSpan OpenTime { get; set; } = new TimeSpan(6, 0, 0);

        public TimeSpan CloseTime { get; set; } = new TimeSpan(22, 0, 0);

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}