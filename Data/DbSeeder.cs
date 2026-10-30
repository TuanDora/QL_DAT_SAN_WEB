//using Microsoft.AspNetCore.Identity;
//using MyCourtBooking.Models;

//namespace MyCourtBooking.Data
//{
//    public static class DbSeeder
//    {
//        public static void SeedData(AppDbContext context)
//        {
//            context.Database.EnsureCreated();

//            // 1. Seed Roles
//            if (!context.Roles.Any())
//            {
//                var roles = new List<Role>
//                {
//                    new Role { Name = "Admin", Description = "Quản trị viên hệ thống" },
//                    new Role { Name = "CourtOwner", Description = "Chủ sân cầu lông" },
//                    new Role { Name = "Customer", Description = "Khách hàng đặt sân" }
//                };
//                context.Roles.AddRange(roles);
//                context.SaveChanges();
//            }

//            // 2. Seed Users
//            if (!context.Users.Any())
//            {
//                var hasher = new PasswordHasher<User>();
//                var adminRole = context.Roles.First(r => r.Name == "Admin");
//                var customerRole = context.Roles.First(r => r.Name == "Customer");

//                var admin = new User
//                {
//                    FullName = "Quản Trị Viên",
//                    Email = "admin@mycourt.vn",
//                    PhoneNumber = "0987654321",
//                    RoleId = adminRole.Id,
//                    IsActive = true,
//                    CreatedAt = DateTime.Now
//                };
//                admin.PasswordHash = hasher.HashPassword(admin, "Admin@123");

//                var customer1 = new User
//                {
//                    FullName = "Nguyễn Văn An",
//                    Email = "nguyenvanan@gmail.com",
//                    PhoneNumber = "0912345678",
//                    RoleId = customerRole.Id,
//                    IsActive = true,
//                    CreatedAt = DateTime.Now
//                };
//                customer1.PasswordHash = hasher.HashPassword(customer1, "123456");

//                context.Users.AddRange(admin, customer1);
//                context.SaveChanges();
//            }

//            // 3. Seed CourtTypes
//            if (!context.CourtTypes.Any())
//            {
//                var types = new List<CourtType>
//                {
//                    new CourtType { Name = "Sân Thảm Tiêu Chuẩn", Description = "Thảm PVC 4.5mm đạt chuẩn thi đấu" },
//                    new CourtType { Name = "Sân Thảm Cao Cấp", Description = "Thảm Yonex chống trơn trượt tuyệt đối" },
//                    new CourtType { Name = "Sân Trong Nhà VIP", Description = "Điều hòa không khí, hệ thống đèn chống chói" }
//                };
//                context.CourtTypes.AddRange(types);
//                context.SaveChanges();
//            }

//            // 4. Seed Courts (10 sân ở Hà Nội)
//            if (!context.Courts.Any())
//            {
//                var standardType = context.CourtTypes.First(t => t.Name == "Sân Thảm Tiêu Chuẩn");
//                var vipType = context.CourtTypes.First(t => t.Name == "Sân Trong Nhà VIP");

//                var courts = new List<Court>
//                {
//                    new Court
//                    {
//                        Name = "Sân Cầu Lông Cầu Giấy Arena",
//                        CourtTypeId = vipType.Id,
//                        Address = "Số 35 Phố Trần Thái Tông, Dịch Vọng",
//                        District = "Cầu Giấy",
//                        City = "Hà Nội",
//                        Description = "Sân mới đầu tư thảm PVC Yonex 100%, có điều hòa không khí, wifi free, chỗ gửi xe ô tô rộng rãi.",
//                        BasePrice = 90000,
//                        Rating = 4.8,
//                        IsActive = true
//                    },
//                    new Court
//                    {
//                        Name = "Sân Cầu Lông Tây Hồ Star",
//                        CourtTypeId = standardType.Id,
//                        Address = "Số 101 Võ Chí Công, Xuân La",
//                        District = "Tây Hồ",
//                        City = "Hà Nội",
//                        Description = "Cụm 6 sân chất lượng cao, thoáng mát, căng vợt tại chỗ.",
//                        BasePrice = 80000,
//                        Rating = 4.6,
//                        IsActive = true
//                    },
//                    new Court
//                    {
//                        Name = "Sân Cầu Lông Thanh Xuân Sport",
//                        CourtTypeId = standardType.Id,
//                        Address = "Số 166 Khuất Duy Tiến, Nhân Chính",
//                        District = "Thanh Xuân",
//                        City = "Hà Nội",
//                        Description = "Trần cao 9m không cản cầu, thảm mới êm chân.",
//                        BasePrice = 85000,
//                        Rating = 4.7,
//                        IsActive = true
//                    },
//                    new Court
//                    {
//                        Name = "Sân Cầu Lông Đống Đa Center",
//                        CourtTypeId = vipType.Id,
//                        Address = "Số 42 Đặng Tiến Đông, Trung Liệt",
//                        District = "Đống Đa",
//                        City = "Hà Nội",
//                        Description = "Sân vip điều hòa mát rượi, hệ thống chiếu sáng chuẩn SEA Games.",
//                        BasePrice = 100000,
//                        Rating = 4.9,
//                        IsActive = true
//                    },
//                    new Court
//                    {
//                        Name = "Sân Cầu Lông Ba Đình Club",
//                        CourtTypeId = standardType.Id,
//                        Address = "Số 12 Quán Thánh, Nguyễn Trung Trực",
//                        District = "Ba Đình",
//                        City = "Hà Nội",
//                        Description = "Không gian sạch đẹp, nhân viên thân thiện nhiệt tình.",
//                        BasePrice = 85000,
//                        Rating = 4.5,
//                        IsActive = true
//                    },
//                    new Court
//                    {
//                        Name = "Sân Cầu Lông Hai Bà Trưng Arena",
//                        CourtTypeId = standardType.Id,
//                        Address = "Số 88 Võ Thị Sáu, Thanh Nhàn",
//                        District = "Hai Bà Trưng",
//                        City = "Hà Nội",
//                        Description = "Có căn tin phục vụ nước uống, thảm xịn chống chấn thương.",
//                        BasePrice = 80000,
//                        Rating = 4.6,
//                        IsActive = true
//                    },
//                    new Court
//                    {
//                        Name = "Sân Cầu Lông Hà Đông Sport",
//                        CourtTypeId = vipType.Id,
//                        Address = "Số 20 Quang Trung, Quang Trung",
//                        District = "Hà Đông",
//                        City = "Hà Nội",
//                        Description = "Cụm 8 sân thi đấu chuyên nghiệp, chỗ đậu xe máy ô tô thoải mái.",
//                        BasePrice = 95000,
//                        Rating = 4.8,
//                        IsActive = true
//                    },
//                    new Court
//                    {
//                        Name = "Sân Cầu Lông Hoàng Mai Club",
//                        CourtTypeId = standardType.Id,
//                        Address = "Số 15 Tân Mai, Tân Mai",
//                        District = "Hoàng Mai",
//                        City = "Hà Nội",
//                        Description = "Giá hợp lý cho học sinh sinh viên, hỗ trợ đặt cố định theo tháng.",
//                        BasePrice = 75000,
//                        Rating = 4.4,
//                        IsActive = true
//                    },
//                    new Court
//                    {
//                        Name = "Sân Cầu Lông Nam Từ Liêm Pro",
//                        CourtTypeId = vipType.Id,
//                        Address = "Số 5 Phạm Hùng, Mỹ Đình",
//                        District = "Nam Từ Liêm",
//                        City = "Hà Nội",
//                        Description = "Sân chuẩn quốc tế, trang thiết bị hiện đại hàng đầu Hà Nội.",
//                        BasePrice = 100000,
//                        Rating = 4.9,
//                        IsActive = true
//                    },
//                    new Court
//                    {
//                        Name = "Sân Cầu Lông Long Biên Zone",
//                        CourtTypeId = standardType.Id,
//                        Address = "Số 10 Nguyễn Văn Cừ, Ngọc Lâm",
//                        District = "Long Biên",
//                        City = "Hà Nội",
//                        Description = "Thoáng mát rộng rãi, ánh sáng tiêu chuẩn không bị chói mắt.",
//                        BasePrice = 80000,
//                        Rating = 4.5,
//                        IsActive = true
//                    }
//                };

//                context.Courts.AddRange(courts);
//                context.SaveChanges();

//                // Seed Court Images & TimeSlotPrices
//                foreach (var court in courts)
//                {
//                    context.CourtImages.Add(new CourtImage
//                    {
//                        CourtId = court.Id,
//                        ImageUrl = "https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?w=800",
//                        IsPrimary = true
//                    });

//                    // Bảng giá khung giờ
//                    context.TimeSlotPrices.AddRange(
//                        new TimeSlotPrice { CourtId = court.Id, StartTime = new TimeSpan(5, 0, 0), EndTime = new TimeSpan(17, 0, 0), Price = court.BasePrice, IsPeakHour = false },
//                        new TimeSlotPrice { CourtId = court.Id, StartTime = new TimeSpan(17, 0, 0), EndTime = new TimeSpan(22, 0, 0), Price = court.BasePrice + 30000, IsPeakHour = true }
//                    );
//                }
//                context.SaveChanges();
//            }

//            // 5. Seed Promotion
//            if (!context.Promotions.Any())
//            {
//                context.Promotions.Add(new Promotion
//                {
//                    Code = "WELCOME10",
//                    DiscountPercent = 10,
//                    MaxDiscountAmount = 50000,
//                    StartDate = DateTime.Now.AddDays(-10),
//                    EndDate = DateTime.Now.AddDays(30),
//                    IsActive = true
//                });
//                context.SaveChanges();
//            }
//        }
//    }
//}