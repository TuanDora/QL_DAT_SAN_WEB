using System;
using System.Collections.Generic;

namespace MyCourtBooking.Modules.Courts
{
    // ViewModel hiển thị thẻ sân ở danh sách
    public class CourtViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CourtTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Status { get; set; } = "Available";
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsActive { get; set; }
    }

    // ViewModel chi tiết sân (bao gồm hình ảnh và đánh giá)
    public class CourtDetailViewModel : CourtViewModel
    {
        public List<string> CourtImages { get; set; } = new List<string>();
        public List<ReviewViewModel> Reviews { get; set; } = new List<ReviewViewModel>();
    }

    public class ReviewViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserAvatar { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CourtTypeViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SlotViewModel
    {
        public int Id { get; set; }
        public int CourtId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal Price { get; set; }
        public bool IsBooked { get; set; }
        public string TimeDisplay => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    }
}