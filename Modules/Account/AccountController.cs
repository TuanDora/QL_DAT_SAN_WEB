using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCourtBooking.Core.Data;
using MyCourtBooking.Core.Entities;
using MyCourtBooking.Core.Enums;
using MyCourtBooking.Modules.Auth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyCourtBooking.Modules.Account
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AccountController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        // ==========================================
        // 1. TRANG HỒ SƠ CÁ NHÂN (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            int userId = GetCurrentUserId();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var model = new UserProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                Avatar = !string.IsNullOrEmpty(user.Avatar) ? user.Avatar : "/images/default-avatar.png",
                RoleName = user.Role ?? "Khách hàng",
                CreatedAt = user.CreatedAt ?? DateTime.Now
            };

            return View("~/Modules/Account/Profile.cshtml", model);
        }

        // ==========================================
        // 2. CẬP NHẬT HỒ SƠ & UPLOAD ANH (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserProfileViewModel model)
        {
            int userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác!");
            }

            if (await _context.Users.AnyAsync(u => u.PhoneNumber == model.PhoneNumber && u.Id != userId))
            {
                ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được sử dụng bởi tài khoản khác!");
            }

            if (!ModelState.IsValid)
            {
                model.Avatar = !string.IsNullOrEmpty(user.Avatar) ? user.Avatar : "/images/default-avatar.png";
                return View("~/Modules/Account/Profile.cshtml", model);
            }

            // Xử lý upload ảnh đại diện mới nếu có chọn tệp
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.AvatarFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AvatarFile.CopyToAsync(fileStream);
                }

                user.Avatar = "/uploads/avatars/" + uniqueFileName;
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            await RefreshUserClaims(user);

            TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
            return RedirectToAction(nameof(Profile));
        }

        // ==========================================
        // 3. ĐỔI MẬT KHẨU (GET & POST)
        // ==========================================
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View("~/Modules/Account/ChangePassword.cshtml", new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Modules/Account/ChangePassword.cshtml", model);
            }

            int userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var hasher = new PasswordHasher<User>();
            var verifyResult = hasher.VerifyHashedPassword(user, user.PasswordHash ?? "", model.CurrentPassword);

            if (verifyResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không chính xác!");
                return View("~/Modules/Account/ChangePassword.cshtml", model);
            }

            user.PasswordHash = hasher.HashPassword(user, model.NewPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu tài khoản thành công!";
            return RedirectToAction(nameof(ChangePassword));
        }

        // ==========================================
        // 4. LỊCH SỬ ĐẶT SÂN & HỦY ĐƠN
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> BookingHistory()
        {
            int userId = GetCurrentUserId();

            var bookings = await _context.Bookings
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Court)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var historyList = new List<BookingHistoryItemViewModel>();

            foreach (var b in bookings)
            {
                var earliestPlayDate = b.BookingDetails.Any()
                    ? b.BookingDetails.Min(d => d.PlayDate.Add(d.StartTime))
                    : DateTime.MinValue;

                bool canCancel = (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                                 && (earliestPlayDate - DateTime.Now).TotalHours >= 24;

                var historyItem = new BookingHistoryItemViewModel
                {
                    BookingId = b.Id,
                    BookingCode = b.BookingCode,
                    BookingDate = b.BookingDate,
                    FinalAmount = b.FinalAmount,
                    Status = b.Status.ToString(),
                    CanCancel = canCancel,
                    Details = new List<BookingDetailItem>()
                };

                foreach (var d in b.BookingDetails)
                {
                    historyItem.Details.Add(new BookingDetailItem
                    {
                        CourtName = d.Court?.Name ?? "Sân đã bị xóa",
                        Address = d.Court != null ? $"{d.Court.Address}, {d.Court.District}" : "",
                        PlayDate = d.PlayDate,
                        StartTime = d.StartTime,
                        EndTime = d.EndTime,
                        Price = d.Price
                    });
                }

                historyList.Add(historyItem);
            }

            return View("~/Modules/Account/BookingHistory.cshtml", historyList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            int userId = GetCurrentUserId();
            var booking = await _context.Bookings
                .Include(b => b.BookingDetails)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn đặt sân!" });
            }

            if (!booking.BookingDetails.Any())
            {
                return Json(new { success = false, message = "Đơn hàng không có thông tin chi tiết!" });
            }

            var earliestPlayDate = booking.BookingDetails.Min(d => d.PlayDate.Add(d.StartTime));
            if ((earliestPlayDate - DateTime.Now).TotalHours < 24)
            {
                return Json(new { success = false, message = "Đơn đặt sân chỉ được phép hủy trước giờ chơi tối thiểu 24 tiếng!" });
            }

            booking.Status = BookingStatus.Cancelled;
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Hủy đơn đặt sân thành công!" });
        }

        private async Task RefreshUserClaims(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email ?? "User"),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "Customer"),
                new Claim("Avatar", !string.IsNullOrEmpty(user.Avatar) ? user.Avatar : "/images/default-avatar.png")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
        }
    }
}