using Microsoft.EntityFrameworkCore;
using MyCourtBooking.Core.Entities;
using MyCourtBooking.Core.Enums;
using MyCourtBooking.Core.Data;
using MyCourtBooking.Models.ViewModels;

namespace MyCourtBooking.Modules.Courts
{
    public class CourtService : ICourtService
    {
        private readonly ApplicationDbContext _context;

        public CourtService(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Lấy tất cả sân (Đồng bộ)
        public List<CourtViewModel> GetAllCourts()
        {
            return _context.Courts
                .Include(c => c.CourtImages)
                .Where(c => c.IsActive)
                .Select(c => MapToViewModel(c))
                .ToList();
        }

        // 2. Lấy sân theo ID (Đồng bộ)
        public CourtViewModel? GetCourtById(int id)
        {
            var court = _context.Courts
                .Include(c => c.CourtImages)
                .FirstOrDefault(c => c.Id == id && c.IsActive);

            return court == null ? null : MapToViewModel(court);
        }

        // 3. Phân trang và tìm kiếm/lọc sân (Bất đồng bộ)
        public async Task<(List<CourtViewModel> Courts, int TotalCount)> GetPagedCourtsAsync(
            string? keyword,
            string? district,
            int? courtTypeId,
            double? minRating,
            decimal? maxPrice,
            int page,
            int pageSize)
        {
            var query = _context.Courts
                .Include(c => c.CourtImages)
                .Where(c => c.IsActive)
                .AsQueryable();

            // 3.1. Tìm theo từ khóa
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(kw) ||
                                         c.Address.ToLower().Contains(kw) ||
                                         c.District.ToLower().Contains(kw));
            }

            // 3.2. Lọc theo Quận/Huyện
            if (!string.IsNullOrWhiteSpace(district) && district != "Tất cả")
            {
                query = query.Where(c => c.District == district);
            }

            // 3.3. Lọc theo Loại sân (Sửa ép kiểu Enum chuẩn EF Core)
            if (courtTypeId.HasValue && courtTypeId.Value > 0)
            {
                var courtTypeEnum = (CourtType)courtTypeId.Value;
                query = query.Where(c => c.CourtType == courtTypeEnum);
            }

            // 3.4. Lọc theo Đánh giá
            if (minRating.HasValue && minRating.Value > 0)
            {
                query = query.Where(c => c.Rating >= minRating.Value);
            }

            // 3.5. Lọc theo Giá
            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                query = query.Where(c => c.BasePrice <= maxPrice.Value);
            }

            int totalCount = await query.CountAsync();

            // 3.6. Thực hiện phân trang và Mapping (Sửa lỗi cú pháp Skip)
            var courts = await query
                .OrderByDescending(c => c.Rating)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => MapToViewModel(c))
                .ToListAsync();

            return (courts, totalCount);
        }

        // 4. Lấy danh sách Quận/Huyện duy nhất
        public async Task<List<string>> GetDistrictsAsync()
        {
            return await _context.Courts
                .Where(c => c.IsActive)
                .Select(c => c.District)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();
        }

        // 5. Lấy danh sách Loại sân
        public async Task<List<CourtTypeViewModel>> GetCourtTypesAsync()
        {
            var courtTypes = Enum.GetValues(typeof(CourtType))
                .Cast<CourtType>()
                .Select(ct => new CourtTypeViewModel
                {
                    Id = (int)ct,
                    Name = ct.ToString()
                })
                .ToList();

            return await Task.FromResult(courtTypes);
        }

        // 6. Lấy chi tiết sân kèm Hình ảnh và Đánh giá
        public async Task<CourtDetailViewModel?> GetCourtDetailByIdAsync(int id)
        {
            var court = await _context.Courts
                .Include(c => c.CourtImages)
                .Include(c => c.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (court == null) return null;

            var firstImg = court.CourtImages.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                           ?? court.CourtImages.FirstOrDefault()?.ImageUrl
                           ?? "https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?w=800";

            var detail = new CourtDetailViewModel
            {
                Id = court.Id,
                Name = court.Name,
                CourtTypeId = (int)court.CourtType,
                TypeName = court.CourtType.ToString(),
                District = court.District,
                Address = court.Address,
                Status = court.IsActive ? "Available" : "Maintenance",
                ImageUrl = firstImg,
                Description = court.Description,
                BasePrice = court.BasePrice,
                Rating = court.Rating,
                ReviewCount = court.Reviews?.Count ?? 0,
                IsActive = court.IsActive,
                CourtImages = court.CourtImages.Select(i => i.ImageUrl).ToList(),
                Reviews = court.Reviews?.OrderByDescending(r => r.CreatedAt).Select(r => new ReviewViewModel
                {
                    Id = r.Id,
                    UserName = r.User?.FullName ?? "Khách hàng",
                    UserAvatar = r.User?.Avatar ?? "/images/default-avatar.png",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToList() ?? new List<ReviewViewModel>()
            };

            return detail;
        }

        // 7. Lấy danh sách sân liên quan cùng khu vực
        public async Task<List<CourtViewModel>> GetRelatedCourtsAsync(int currentCourtId, string district, int count)
        {
            return await _context.Courts
                .Include(c => c.CourtImages)
                .Where(c => c.IsActive && c.Id != currentCourtId && c.District == district)
                .Take(count)
                .Select(c => MapToViewModel(c))
                .ToListAsync();
        }

        // 8. Lấy danh sách khung giờ (Slots) còn trống trong ngày
        public async Task<List<SlotViewModel>> GetAvailableSlotsAsync(int courtId, DateTime date)
        {
            // 1. Lấy danh sách các khung giờ (StartTime kiểu TimeSpan) đã bị đặt trong ngày từ CSDL
            var bookedStartTimes = await _context.BookingDetails
                .Where(bd => bd.CourtId == courtId
                          && bd.PlayDate.Date == date.Date
                          && bd.Booking != null
                          && bd.Booking.Status != BookingStatus.Cancelled)
                .Select(bd => bd.StartTime)
                .ToListAsync();

            // 2. Sinh 48 slot (30 phút/slot) từ 00:00 đến 23:30 bằng kiểu TimeSpan chuẩn
            var slots = new List<SlotViewModel>();
            var currentTime = TimeSpan.Zero;

            for (int i = 0; i < 48; i++)
            {
                var nextTime = currentTime.Add(TimeSpan.FromMinutes(30));

                // Tính giá tiền mặc định theo bảng giá từng khung giờ
                decimal price = 65000;
                if (currentTime >= new TimeSpan(0, 0, 0) && currentTime < new TimeSpan(6, 0, 0))
                    price = 75000;
                else if (currentTime >= new TimeSpan(6, 0, 0) && currentTime < new TimeSpan(16, 0, 0))
                    price = 65000;
                else if (currentTime >= new TimeSpan(16, 0, 0) && currentTime < new TimeSpan(22, 0, 0))
                    price = 140000;
                else if (currentTime >= new TimeSpan(22, 0, 0))
                    price = 90000;

                bool isBooked = bookedStartTimes.Contains(currentTime);

                slots.Add(new SlotViewModel
                {
                    Id = i + 1,
                    CourtId = courtId,
                    StartTime = currentTime,
                    EndTime = nextTime,
                    Price = price,
                    IsBooked = isBooked
                });

                currentTime = nextTime;
            }

            return slots;
        }

        // 9. Thêm đánh giá cho sân
        public async Task<(bool Success, string Message)> AddReviewAsync(int userId, int courtId, double rating, string comment)
        {
            var court = await _context.Courts.Include(c => c.Reviews).FirstOrDefaultAsync(c => c.Id == courtId);
            if (court == null)
            {
                return (false, "Sân không tồn tại!");
            }

            var review = new Review
            {
                UserId = userId,
                CourtId = courtId,
                Rating = (int)rating,
                Comment = comment,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            var reviews = await _context.Reviews.Where(r => r.CourtId == courtId).ToListAsync();
            court.Rating = reviews.Average(r => r.Rating);
            _context.Courts.Update(court);
            await _context.SaveChangesAsync();

            return (true, "Gửi đánh giá thành công!");
        }

        // Helper private map Entity -> ViewModel
        private static CourtViewModel MapToViewModel(Court c)
        {
            var img = c.CourtImages?.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                      ?? c.CourtImages?.FirstOrDefault()?.ImageUrl
                      ?? "https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?w=800";

            return new CourtViewModel
            {
                Id = c.Id,
                Name = c.Name,
                CourtTypeId = (int)c.CourtType,
                TypeName = c.CourtType.ToString(),
                District = c.District,
                Address = c.Address,
                Status = c.IsActive ? "Available" : "Maintenance",
                ImageUrl = img,
                Description = c.Description,
                BasePrice = c.BasePrice,
                Rating = c.Rating,
                ReviewCount = c.Reviews?.Count ?? 0,
                IsActive = c.IsActive
            };
        }
    }
}