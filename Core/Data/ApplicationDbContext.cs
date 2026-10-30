using System;
using Microsoft.EntityFrameworkCore;
using MyCourtBooking.Core.Entities;
using MyCourtBooking.Core.Enums;

namespace MyCourtBooking.Core.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Court> Courts { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<BookingDetail> BookingDetails { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<TimeSlotPrice> TimeSlotPrices { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Service> Services { get; set; }
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Unique Index cho Email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // 2. Định dạng kiểu decimal cho giá tiền
            modelBuilder.Entity<Court>()
                .Property(c => c.PricePerHour)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalPrice)
                .HasPrecision(18, 2);

            // 3. Cấu hình Quan hệ & Khóa ngoại
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Court)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.CourtId)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. Seed Data Ban Đầu
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed tài khoản Admin mẫu (Mật khẩu hash giản lược hoặc chuẩn BCrypt)
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    FullName = "Quản trị viên Hệ thống",
                    Email = "admin@mycourtbooking.com",
                    PasswordHash = "AQAAAAEAACcQAAAAEH8zQ1==", // Mật khẩu mẫu Admin@123
                    PhoneNumber = "0901234567",
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1)
                },
                new User
                {
                    Id = 2,
                    FullName = "Nguyễn Văn Khách",
                    Email = "khachhang@gmail.com",
                    PasswordHash = "AQAAAAEAACcQAAAAEH8zQ1==", // Mật khẩu mẫu Customer@123
                    PhoneNumber = "0987654321",
                    Role = "Customer",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 2)
                }
            );

            // Seed danh sách sân cầu lông mẫu
            modelBuilder.Entity<Court>().HasData(
                new Court
                {
                    Id = 1,
                    Name = "Sân Cầu Lông 01 - Thảm Yonex VIP",
                    Description = "Sân thảm Yonex chống trơn trượt đạt tiêu chuẩn thi đấu quốc tế, hệ thống đèn chiếu sáng LED hiện đại không lóa mắt.",
                    Location = "Tầng 2, Nhà thi đấu Cầu Giấy, Hà Nội",
                    PricePerHour = 120000m,
                    CourtType = CourtType.VIP,
                    ImageUrl = "/images/courts/court-1.jpg",
                    IsActive = true,
                    OpenTime = new TimeSpan(6, 0, 0),
                    CloseTime = new TimeSpan(22, 0, 0)
                },
                new Court
                {
                    Id = 2,
                    Name = "Sân Cầu Lông 02 - Trong Nhà Standard",
                    Description = "Sân trong nhà thoáng mát, trần cao 9m đạt chuẩn, không khí lưu thông tốt.",
                    Location = "Tầng 2, Nhà thi đấu Cầu Giấy, Hà Nội",
                    PricePerHour = 90000m,
                    CourtType = CourtType.Indoor,
                    ImageUrl = "/images/courts/court-2.jpg",
                    IsActive = true,
                    OpenTime = new TimeSpan(6, 0, 0),
                    CloseTime = new TimeSpan(22, 0, 0)
                },
                new Court
                {
                    Id = 3,
                    Name = "Sân Cầu Lông 03 - Trong Nhà Standard",
                    Description = "Sân tiêu chuẩn tập luyện hàng ngày, trang bị sẵn ghế ngồi nghỉ và hệ thống quạt mát công suất lớn.",
                    Location = "Tầng 2, Nhà thi đấu Cầu Giấy, Hà Nội",
                    PricePerHour = 90000m,
                    CourtType = CourtType.Indoor,
                    ImageUrl = "/images/courts/court-3.jpg",
                    IsActive = true,
                    OpenTime = new TimeSpan(6, 0, 0),
                    CloseTime = new TimeSpan(22, 0, 0)
                },
                new Court
                {
                    Id = 4,
                    Name = "Sân Cầu Lông 04 - Ngoài Trời Thảm Caosu",
                    Description = "Sân ngoài trời có mái che di động, không gian thoáng đãng thích hợp giao lưu buổi sáng và chiều tối.",
                    Location = "Khu liên hợp thể thao Mỹ Đình, Hà Nội",
                    PricePerHour = 70000m,
                    CourtType = CourtType.Outdoor,
                    ImageUrl = "/images/courts/court-4.jpg",
                    IsActive = true,
                    OpenTime = new TimeSpan(6, 0, 0),
                    CloseTime = new TimeSpan(21, 0, 0)
                }
            );
        }
    }
}