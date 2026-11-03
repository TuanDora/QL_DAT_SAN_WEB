using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCourtBooking.Models.Data;
using MyCourtBooking.Models.Entities;
using MyCourtBooking.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyCourtBooking.Modules.Admin
{
    //[Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DASHBOARD TỔNG QUAN (/Admin)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalCourts = await _context.Courts.CountAsync(c => c.IsActive);
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.PendingBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Pending);

            var confirmedBookings = await _context.Bookings
                .Where(b => b.Status == BookingStatus.Confirmed)
                .ToListAsync();

            decimal totalRevenue = confirmedBookings.Sum(b => b.FinalAmount > 0 ? b.FinalAmount : b.TotalAmount);
            ViewBag.TotalRevenue = totalRevenue;

            // Thống kê Doanh thu 7 ngày gần nhất
            var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(-6 + i)).ToList();
            ViewBag.DaysLabels = last7Days.Select(d => d.ToString("dd/MM/yyyy")).ToList();
            ViewBag.DaysData = last7Days.Select(day =>
                confirmedBookings.Where(b => b.BookingDate.Date == day.Date)
                                 .Sum(b => b.FinalAmount > 0 ? b.FinalAmount : b.TotalAmount)
            ).ToList();

            // Thống kê Doanh thu 4 tuần gần nhất
            var weekLabels = new List<string>();
            var weekData = new List<decimal>();
            for (int i = 3; i >= 0; i--)
            {
                var endOfWeek = DateTime.Today.AddDays(-i * 7);
                var startOfWeek = endOfWeek.AddDays(-6);
                weekLabels.Add($"{startOfWeek:dd/MM} - {endOfWeek:dd/MM}");

                var revenue = confirmedBookings
                    .Where(b => b.BookingDate.Date >= startOfWeek.Date && b.BookingDate.Date <= endOfWeek.Date)
                    .Sum(b => b.FinalAmount > 0 ? b.FinalAmount : b.TotalAmount);
                weekData.Add(revenue);
            }
            ViewBag.WeeksLabels = weekLabels;
            ViewBag.WeeksData = weekData;

            // Thống kê Doanh thu 6 tháng gần nhất
            var monthsLabels = Enumerable.Range(0, 6).Select(i => DateTime.Today.AddMonths(-5 + i)).ToList();
            ViewBag.MonthsLabels = monthsLabels.Select(m => m.ToString("MM/yyyy")).ToList();
            ViewBag.MonthsData = monthsLabels.Select(m =>
                confirmedBookings.Where(b => b.BookingDate.Month == m.Month && b.BookingDate.Year == m.Year)
                                 .Sum(b => b.FinalAmount > 0 ? b.FinalAmount : b.TotalAmount)
            ).ToList();

            return View("~/Modules/Admin/Index.cshtml");
        }

        // ==========================================
        // 2. QUẢN LÝ DANH SÁCH SÂN (/Admin/ManageCourts)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ManageCourts(string? searchString, string? statusFilter)
        {
            var courtsQuery = _context.Courts
                
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                string kw = searchString.Trim().ToLower();
                courtsQuery = courtsQuery.Where(c => c.Name.ToLower().Contains(kw) ||
                                                     c.District.ToLower().Contains(kw) ||
                                                     (c.CourtType != null && c.CourtType.ToString().ToLower().Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                bool isActive = statusFilter == "Available";
                courtsQuery = courtsQuery.Where(c => c.IsActive == isActive);
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;
            var courtTypes = Enum.GetValues(typeof(CourtType))
                      .Cast<CourtType>()
                      .Select(e => new { Id = (int)e, Name = e.ToString() })
                      .ToList();

            var courts = await courtsQuery.OrderByDescending(c => c.Id).ToListAsync();
            return View("~/Modules/Admin/ManageCourts.cshtml", courts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourt(Court court)
        {
            if (court.BasePrice <= 0) court.BasePrice = 120000;
            court.IsActive = true;

            _context.Courts.Add(court);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm sân mới thành công!";
            return RedirectToAction(nameof(ManageCourts));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourt(Court court)
        {
            var existingCourt = await _context.Courts.FindAsync(court.Id);
            if (existingCourt != null)
            {
                existingCourt.Name = court.Name;
                existingCourt.CourtTypeId = court.CourtTypeId;
                existingCourt.District = court.District;
                existingCourt.Address = court.Address;
                existingCourt.BasePrice = court.BasePrice;
                existingCourt.Description = court.Description;
                existingCourt.IsActive = court.IsActive;

                _context.Courts.Update(existingCourt);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật thông tin sân '{court.Name}' thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy sân cần cập nhật!";
            }
            return RedirectToAction(nameof(ManageCourts));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourt(int id)
        {
            var court = await _context.Courts.FindAsync(id);
            if (court != null)
            {
                court.IsActive = false; // Soft delete
                _context.Courts.Update(court);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã ngưng hoạt động sân thành công!";
            }
            return RedirectToAction(nameof(ManageCourts));
        }

        // ==========================================
        // 3. QUẢN LÝ ĐẶT SÂN (/Admin/ManageBookings)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ManageBookings(string? searchString, string? statusFilter)
        {
            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Court)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                string kw = searchString.Trim().ToLower();
                query = query.Where(b => (b.CustomerName != null && b.CustomerName.ToLower().Contains(kw)) ||
                                         (b.CustomerPhone != null && b.CustomerPhone.Contains(kw)) ||
                                         (b.BookingCode != null && b.BookingCode.ToLower().Contains(kw)));
            }
            if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<BookingStatus>(statusFilter, true, out var parsedStatus))
            {
                query = query.Where(b => b.Status == parsedStatus);
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;

            var bookings = await query.OrderByDescending(b => b.Id).ToListAsync();
            return View("~/Modules/Admin/ManageBookings.cshtml", bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, BookingStatus status)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking != null)
            {
                booking.Status = status;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng #{booking.BookingCode ?? bookingId.ToString()} thành '{status}'!";
            }
            return RedirectToAction(nameof(ManageBookings));
        }

        // ==========================================
        // 4. QUẢN LÝ TÀI KHOẢN & PHÂN QUYỀN (/Admin/ManageUsers)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ManageUsers(string? searchString)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                string kw = searchString.Trim().ToLower();
                query = query.Where(u => (u.FullName != null && u.FullName.ToLower().Contains(kw)) ||
                                         (u.Email != null && u.Email.ToLower().Contains(kw)) ||
                                         (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)));
            }

            var users = await query.OrderByDescending(u => u.Id).ToListAsync();
            return View("~/Modules/Admin/ManageUsers.cshtml", users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã {((user.IsActive ?? false) ? "kích hoạt" : "khóa")} tài khoản '{user.FullName}' thành công!";
            }
            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(string fullName, string email, string phoneNumber, string role)
        {
            var newUser = new User
            {
                FullName = fullName,
                Email = email,
                PhoneNumber = phoneNumber,
                Role = string.IsNullOrWhiteSpace(role) ? "Staff" : role,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm nhân sự '{fullName}' thành công!";
            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, string fullName, string email, string phoneNumber, string role)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.FullName = fullName;
                user.Email = email;
                user.PhoneNumber = phoneNumber;
                if (!string.IsNullOrWhiteSpace(role)) user.Role = role;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật thông tin tài khoản #{id} thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản cần chỉnh sửa!";
            }
            return RedirectToAction(nameof(ManageUsers));
        }

        // ==========================================
        // 5. BẢNG GIÁ & CẤU HÌNH GIỜ VÀNG (/Admin/Pricing)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Pricing()
        {
            var courtTypes = Enum.GetValues(typeof(CourtType))
                     .Cast<CourtType>()
                     .Select(e => new { Id = (int)e, Name = e.ToString() })
                     .ToList();

            var courts = await _context.Courts.ToListAsync();
            var pricingList = Enum.GetValues(typeof(CourtType))
                .Cast<CourtType>()
                .Select(type => new CourtTypePricingViewModel
                {
                    TypeId = (int)type,
                    Type = type.ToString(),
                    CourtCount = courts.Count(c => c.CourtType == type),
                    PricePerHour = courts.FirstOrDefault(c => c.CourtType == type)?.PricePerHour ?? 100000m,
                    PeakPricePerHour = (courts.FirstOrDefault(c => c.CourtType == type)?.PricePerHour ?? 100000m) * 1.3m
                }).ToList();

            return View("~/Modules/Admin/Pricing.cshtml", pricingList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTypePricing(int typeId, decimal pricePerHour)
        {
            var courtsOfType = await _context.Courts.Where(c => c.CourtTypeId == typeId).ToListAsync();
            foreach (var court in courtsOfType)
            {
                court.BasePrice = pricePerHour;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật biểu giá thành công!";
            return RedirectToAction(nameof(Pricing));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveGoldenHour(string goldenHourStart, string goldenHourEnd, bool goldenHourActive)
        {
            TempData["SuccessMessage"] = $"Đã lưu khung Giờ vàng ({goldenHourStart} - {goldenHourEnd}) thành công!";
            return RedirectToAction(nameof(Pricing));
        }

        // ==========================================
        // 6. CẤU HÌNH HỆ THỐNG (/Admin/Settings)
        // ==========================================
        [HttpGet]
        public IActionResult Settings()
        {
            ViewBag.SystemName = TempData["SystemName"]?.ToString() ?? "Hệ thống Quản lý Sân Thể Thao";
            ViewBag.Hotline = TempData["Hotline"]?.ToString() ?? "0901234567";
            ViewBag.Email = TempData["Email"]?.ToString() ?? "admin@sanethethao.vn";
            ViewBag.Address = TempData["Address"]?.ToString() ?? "123 Đường Cầu Giấy, Hà Nội";
            ViewBag.OpenTime = TempData["OpenTime"]?.ToString() ?? "06:00";
            ViewBag.CloseTime = TempData["CloseTime"]?.ToString() ?? "22:30";
            ViewBag.BankName = TempData["BankName"]?.ToString() ?? "MBBank";
            ViewBag.AccountNumber = TempData["AccountNumber"]?.ToString() ?? "0901234567999";
            ViewBag.AccountHolder = TempData["AccountHolder"]?.ToString() ?? "NGUYEN VAN A";
            ViewBag.MaintenanceMode = TempData["MaintenanceMode"] != null && Convert.ToBoolean(TempData["MaintenanceMode"]);

            return View("~/Modules/Admin/Settings.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveSettings(string systemName, string hotline, string email, string address, string openTime, string closeTime, string bankName, string accountNumber, string accountHolder, bool maintenanceMode)
        {
            TempData["SystemName"] = systemName;
            TempData["Hotline"] = hotline;
            TempData["Email"] = email;
            TempData["Address"] = address;
            TempData["OpenTime"] = openTime;
            TempData["CloseTime"] = closeTime;
            TempData["BankName"] = bankName;
            TempData["AccountNumber"] = accountNumber;
            TempData["AccountHolder"] = accountHolder;
            TempData["MaintenanceMode"] = maintenanceMode;

            TempData["SuccessMessage"] = maintenanceMode
                ? "Đã bật chế độ BẢO TRÌ toàn hệ thống!"
                : "Đã lưu toàn bộ cài đặt hệ thống thành công!";

            return RedirectToAction(nameof(Settings));
        }

        // ==========================================
        // 7. BÁO CÁO & THỐNG KÊ (/Admin/Reports)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            ViewBag.TotalCourts = await _context.Courts.CountAsync(c => c.IsActive);
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();

            var confirmedBookings = await _context.Bookings
                .Where(b => b.Status == BookingStatus.Confirmed)
                .ToListAsync();

            decimal totalRevenue = confirmedBookings.Sum(b => b.FinalAmount > 0 ? b.FinalAmount : b.TotalAmount);
            ViewBag.TotalRevenue = totalRevenue;

            return View("~/Modules/Admin/Reports.cshtml");
        }

        // ==========================================
        // 8. NHẬT KÝ HOẠT ĐỘNG (/Admin/AuditLogs)
        // ==========================================
        [HttpGet]
        public IActionResult AuditLogs()
        {
            return View("~/Modules/Admin/AuditLogs.cshtml");
        }
        // ==========================================
        // 9. QUẢN LÝ DỊCH VỤ & KHO HÀNG (/Admin/ManageServices)
        // ==========================================
        // 1. LẤY DANH SÁCH DỊCH VỤ TỪ DATABASE
        [HttpGet]
        public async Task<IActionResult> ManageServices()
        {
            var services = await _context.Services
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            return View("~/Modules/Admin/ManageServices.cshtml", services);
        }

        // 2. LƯU DỊCH VỤ MỚI VÀO DATABASE SQL SERVER
        [HttpPost]
        public async Task<IActionResult> CreateService(string Name, string Category, string Unit, decimal Price, int Stock)
        {
            var newService = new Service
            {
                Name = Name,
                Category = Category,
                Unit = Unit,
                Price = Price,
                Stock = Stock,
                IsActive = true
            };

            // BẮT BUỘC BẢO ĐẢM 2 DÒNG NÀY ĐƯỢC THỰC THI (KHÔNG CÓ DẤU COMMENT /* */)
            _context.Services.Add(newService);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm thành công dịch vụ/sản phẩm '{Name}'!";
            return RedirectToAction(nameof(ManageServices));
        }
        // ==========================================
        // CẬP NHẬT DỊCH VỤ / SẢN PHẨM
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> EditService(int Id, string Name, string Category, string Unit, decimal Price, int Stock)
        {
            var service = await _context.Services.FindAsync(Id);
            if (service == null) return NotFound();

            service.Name = Name;
            service.Category = Category;
            service.Unit = Unit;
            service.Price = Price;
            service.Stock = Stock;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật dịch vụ/sản phẩm '{Name}'!";
            return RedirectToAction(nameof(ManageServices));
        }

        // ==========================================
        // XÓA DỊCH VỤ / SẢN PHẨM (Soft Delete)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                // Chuyển IsActive = false để ẩn khỏi danh sách thay vì xóa hẳn khỏi database
                service.IsActive = false;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã xóa dịch vụ/sản phẩm '{service.Name}'!";
            }

            return RedirectToAction(nameof(ManageServices));
        }
    }

    public class CourtTypePricingViewModel
    {
        public int TypeId { get; set; }
        public string Type { get; set; } = string.Empty;
        public int CourtCount { get; set; }
        public decimal PricePerHour { get; set; }
        public decimal PeakPricePerHour { get; set; }
    }
    public class CourtReportViewModel
    {
        public string CourtName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public int TotalBookings { get; set; }
        public int OccupancyRate { get; set; }
        public decimal Revenue { get; set; }
    }
}