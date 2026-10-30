using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCourtBooking.Core.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyCourtBooking.Modules.Home
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? keyword, string? district, double? minRating, decimal? maxPrice)
        {
            // 1. Thống kê tổng số sân (dành cho Banner 1)
            ViewBag.TotalCourts = await _context.Courts.CountAsync(c => c.IsActive);

            // 2. Lấy danh sách các Quận/Huyện duy nhất nạp vào Dropdown
            ViewBag.Districts = await _context.Courts
                .Select(c => c.District)
                .Where(d => !string.IsNullOrEmpty(d))
                .Distinct()
                .ToListAsync();

            // 3. Lấy dữ liệu cơ bản từ CSDL kèm hình ảnh và đưa về bộ nhớ (AsEnumerable)
            // Giúp tránh hoàn toàn lỗi dịch LINQ to SQL của EF Core
            var listCourts = await _context.Courts
                .Include(c => c.CourtImages)
                .Where(c => c.IsActive)
                .ToListAsync();

            var query = listCourts.AsQueryable();

            // 4. Lọc theo Từ khóa (Tên sân hoặc Địa chỉ)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                query = query.Where(c => (!string.IsNullOrEmpty(c.Name) && c.Name.ToLower().Contains(kw)) ||
                                         (!string.IsNullOrEmpty(c.Address) && c.Address.ToLower().Contains(kw)) ||
                                         (!string.IsNullOrEmpty(c.District) && c.District.ToLower().Contains(kw)));
            }

            // 5. Lọc theo Quận/Huyện
            if (!string.IsNullOrWhiteSpace(district) && district != "Tất cả")
            {
                query = query.Where(c => c.District == district);
            }

            // 6. Lọc theo Đánh giá
            if (minRating.HasValue && minRating.Value > 0)
            {
                double ratingVal = minRating.Value;
                query = query.Where(c => c.Rating >= ratingVal);
            }

            // 7. Lọc theo Giá tối đa
            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                decimal priceVal = maxPrice.Value;
                query = query.Where(c => c.BasePrice <= priceVal);
            }

            // 8. Lưu lại trạng thái bộ lọc trên giao diện
            ViewBag.Keyword = keyword;
            ViewBag.SelectedDistrict = district;
            ViewBag.MinRating = minRating;
            ViewBag.MaxPrice = maxPrice;

            var courts = query.OrderByDescending(c => c.Rating).ToList();
            return View(courts);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}