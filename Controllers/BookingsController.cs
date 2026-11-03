using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyCourtBooking.Models.Data;
using MyCourtBooking.Models.Enums;
using MyCourtBooking.Models;
using MyCourtBooking.Models.ViewModels;
using MyCourtBooking.Models.Services;
using System;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MyCourtBooking.Modules.Bookings;

namespace MyCourtBooking.Controllers
{
    [AllowAnonymous]
    public class BookingsController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        // 2. Khởi tạo Constructor để ASP.NET Core tự động nạp Database vào Controller
        // 2. Khởi tạo Constructor để ASP.NET Core tự động nạp Database và Service vào Controller
        public BookingsController(ApplicationDbContext context, IBookingService bookingService, IConfiguration configuration)
        {
            _context = context;
            _bookingService = bookingService;
            _configuration = configuration;
        }



        // ==========================================
        // 1. TRANG CHỌN KHUNG GIỜ & ĐẶT SÂN
        // ==========================================
        // GET: /Bookings/SelectSlot?courtId=1&date=2026-10-25&slotId=5
        [HttpGet]
        public async Task<IActionResult> SelectSlot(int courtId, DateTime? date)
        {
            DateTime selectedDate = date ?? DateTime.Today;

            var court = await _context.Courts.FindAsync(courtId);
            if (court == null) return NotFound();

            // ==========================================================
            // 1. TÌM CÁC KHUNG GIỜ ĐÃ ĐƯỢC ĐẶT (Đã sửa lỗi SlotId)
            // Lấy danh sách giờ bắt đầu & kết thúc của các đơn đặt sân
            // ==========================================================
            var bookedTimes = await _context.Bookings
                .Where(b => b.CourtId == courtId && b.BookingDate.Date == selectedDate.Date && b.Status != BookingStatus.Cancelled)
                .Select(b => new { b.StartTime, b.EndTime }) // Trích xuất StartTime và EndTime từ Database
                .ToListAsync();

            // ==========================================================
            // 2. TỰ ĐỘNG TẠO SƠ ĐỒ 48 SLOT ẢO (Xóa bỏ lỗi _context.TimeSlots)
            // ==========================================================
            var allSlots = new List<Models.DTOs.SlotViewModel>();

            TimeSpan openTime = new TimeSpan(5, 30, 0);  // Sân mở cửa 05:30
            TimeSpan closeTime = new TimeSpan(23, 0, 0); // Sân đóng cửa 23:00
            TimeSpan currentStartTime = openTime;

            while (currentStartTime < closeTime)
            {
                TimeSpan currentEndTime = currentStartTime.Add(TimeSpan.FromMinutes(30));
                int currentSlotIndex = (int)currentStartTime.TotalMinutes / 30 + 1;

                // KIỂM TRA TRẠNG THÁI KHUNG GIỜ (Trống / Đã đặt / Đã qua)
                // Khung giờ bị trùng nếu nó nằm bên trong bất kỳ thời gian Booking nào đã lấy ở Bước 1
                bool isBooked = bookedTimes.Any(b => currentStartTime >= b.StartTime && currentStartTime < b.EndTime);
                bool isPast = (selectedDate.Date == DateTime.Today) && (currentStartTime <= DateTime.Now.TimeOfDay);

                string status = "empty";
                if (isBooked) status = "booked";
                else if (isPast) status = "locked";

                // TÍNH GIÁ TIỀN CHUẨN XÁC THEO KHUNG GIỜ
                decimal price = 65000;
                if (currentStartTime.Hours < 6) price = 75000; // 00:00 - 06:00
                else if (currentStartTime.Hours >= 6 && currentStartTime.Hours < 16) price = 65000; // 06:00 - 16:00
                else if (currentStartTime.Hours >= 16 && currentStartTime.Hours < 22) price = 140000; // 16:00 - 22:00
                else if (currentStartTime.Hours >= 22) price = 90000; // 22:00 - 24:00

                // Thêm Slot vào danh sách hiển thị cho View
                allSlots.Add(new Modules.Bookings.SlotViewModel
                {
                    CourtId = court.Id,
                    CourtName = court.Name,
                    SlotIndex = currentSlotIndex,
                    StartTime = currentStartTime.ToString(@"hh\:mm"),
                    EndTime = currentEndTime.ToString(@"hh\:mm"),
                    Status = status,
                    PricePerSlot = price
                });

                // Tăng lên 30 phút để kiểm tra khung giờ tiếp theo
                currentStartTime = currentEndTime;
            }

            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");
            ViewBag.CourtPhone = "0912345678";
            ViewBag.CourtZalo = "0912345678";
            // LẤY DANH SÁCH DỊCH VỤ/SẢN PHẨM CÒN HÀNG TỪ DATABASE
            var activeServices = await _context.Services
                .Where(s => s.IsActive && s.Stock > 0)
                .ToListAsync();
            ViewBag.Services = activeServices;

            return View(allSlots);

            
        }

        // ==========================================
        // 2. TẠO ĐƠN ĐẶT SÂN QUA AJAX
        // ==========================================
        // POST: /Bookings/CreateBooking
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu đặt sân không hợp lệ."
                });
            }

            // Tự động lấy UserId nếu người dùng đã đăng nhập hệ thống
            int? userId = null;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // Tìm Claim chứa ID theo nhiều chuẩn khác nhau (NameIdentifier, UserId, Sub)
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("UserId")
                           ?? User.FindFirst("id");

                if (idClaim != null && int.TryParse(idClaim.Value, out int parsedUserId))
                {
                    userId = parsedUserId;
                }
            }

            // Chuẩn hóa số điện thoại, bỏ khoảng trắng và dấu gạch ngang
            string phone = Regex.Replace(request.CustomerPhone ?? "", @"[\s\-]", "");

            // Kiểm tra số điện thoại Việt Nam 10 chữ số bắt đầu bằng số 0
            if (!Regex.IsMatch(phone, @"^0\d{9}$"))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng nhập số điện thoại hợp lệ gồm 10 chữ số (bắt đầu bằng 0)."
                });
            }

            request.CustomerPhone = phone;

            var result = await _bookingService.CreateBookingAsync(request, userId);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Json(new
            {
                success = true,
                message = result.Message,
                bookingCode = result.BookingCode,
                durationHours = result.DurationHours,
                totalPrice = result.TotalPrice,
                startTime = result.StartTime,
                endTime = result.EndTime
            });
        }
        // ==========================================
        // 3. TRANG DANH SÁCH ĐƠN ĐẶT CỦA TÔI
        // ==========================================
        // GET: /Bookings/MyBookings
        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            int? userId = null;
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                       ?? User.FindFirst("UserId")
                       ?? User.FindFirst("id");

            if (idClaim != null && int.TryParse(idClaim.Value, out int parsedUserId))
            {
                userId = parsedUserId;
            }

            if (!userId.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var rawBookings = await _bookingService.GetUserBookingsAsync(userId.Value);

            // Ánh xạ dữ liệu sang ViewModel sạch cho View
            var viewModels = rawBookings.Select(b => new MyBookingItemViewModel
            {
                Id = b.Id,
                BookingCode = b.BookingCode,
                CreatedAt = b.CreatedAt,
                StatusText = b.Status.ToString().Contains("Confirm") ? "Đã xác nhận" :
                             (b.Status.ToString().Contains("Cancel") ? "Đã hủy" : "Chờ xử lý"),
                StatusClass = b.Status.ToString().Contains("Confirm") ? "bg-success" :
                              (b.Status.ToString().Contains("Cancel") ? "bg-danger" : "bg-warning text-dark"),
                TotalAmount = b.TotalAmount,
                CustomerPhone = b.CustomerPhone,
                Details = b.BookingDetails?.Select(d => new MyBookingDetailItem
                {
                    CourtName = d.Court?.Name ?? "Sân cầu lông",
                    PlayDate = d.PlayDate,
                    StartTime = d.StartTime.ToString(@"hh\:mm"),
                    EndTime = d.EndTime.ToString(@"hh\:mm")
                }).ToList() ?? new List<MyBookingDetailItem>()
            }).ToList();

            return View("~/Modules/Bookings/MyBookings.cshtml", viewModels);
        }
    }
}