using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCourtBooking.Core.Data;
using MyCourtBooking.Core.Entities;
using MyCourtBooking.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyCourtBooking.Modules.Auth
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        // Constructor phải gán giá trị cho cả _authService và _context
        public AuthController(IAuthService authService, ApplicationDbContext context, IEmailService emailService)
        {
            _authService = authService;
            _context = context;
            _emailService = emailService;
        }
        // 1. XỬ LÝ QUÊN MẬT KHẨU (GỬI MAIL)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string Email)
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập địa chỉ email!";
                return View("~/Modules/Auth/ForgotPassword.cshtml");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (user == null)
            {
                ViewBag.ErrorMessage = "Email này chưa được đăng ký trong hệ thống!";
                return View("~/Modules/Auth/ForgotPassword.cshtml");
            }

            try
            {
                string resetToken = Guid.NewGuid().ToString();
                string resetLink = Url.Action("ResetPassword", "Auth", new { token = resetToken, email = Email }, Request.Scheme)!;

                string body = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                <h3 style='color: #0d6efd;'>Khôi phục mật khẩu - MyCourtBooking</h3>
                <p>Xin chào <b>{user.FullName ?? user.Email}</b>,</p>
                <p>Vui lòng bấm vào nút bên dưới để tiến hành tạo mật khẩu mới:</p>
                <p style='margin: 20px 0;'>
                    <a href='{resetLink}' style='background-color: #0d6efd; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Đặt lại mật khẩu</a>
                </p>
            </div>";

                await _emailService.SendEmailAsync(Email, "Khôi phục mật khẩu MyCourtBooking", body);

                // Gán thông báo vào cả ViewBag và TempData
                string successMsg = "Yêu cầu khôi phục mật khẩu đã được gửi đến email của bạn!";
                ViewBag.SuccessMessage = successMsg;
                TempData["SuccessMessage"] = successMsg;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Không thể gửi email: " + ex.Message;
            }

            return View("~/Modules/Auth/ForgotPassword.cshtml");
        }

        // 2. TRANG ĐẶT LẠI MẬT KHẨU KHI BẤM LINK TỪ EMAIL (GET) -> SỬA LỖI 404
        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View("~/Modules/Auth/ResetPassword.cshtml", model);
        }

        // 3. XỬ LÝ LƯU MẬT KHẨU MỚI (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Modules/Auth/ResetPassword.cshtml", model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                ViewBag.ErrorMessage = "Tài khoản không tồn tại trên hệ thống!";
                return View("~/Modules/Auth/ResetPassword.cshtml", model);
            }

            // Hash mật khẩu mới và lưu vào DB
            var hasher = new PasswordHasher<User>();
            user.PasswordHash = hasher.HashPassword(user, model.NewPassword);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập lại.";
            return RedirectToAction("Login", "Auth");
        }
        public class LoginViewModel
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string? ReturnUrl { get; set; }
        }

        // Chặn lỗi và tự động bật lại Modal đăng nhập nếu người dùng lỡ gõ trực tiếp /Auth/Login trên trình duyệt
        [HttpGet]
        public IActionResult Login()
        {
            return Redirect("/?openAuth=true");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Xử lý xác thực thông qua Service
            var user = await _authService.AuthenticateAsync(model.Email, model.Password);

            if (user == null)
            {
                TempData["AuthError"] = "Email/Số điện thoại hoặc mật khẩu không chính xác!";
                return Redirect(string.IsNullOrEmpty(model.ReturnUrl) ? "/?openAuth=true" : model.ReturnUrl);
            }

            // Gán các thông tin định danh (Claims) cho tài khoản, bao gồm phân quyền Role
            var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Name, user.FullName ?? user.Email ?? "User"),
    new Claim(ClaimTypes.Email, user.Email ?? ""),
    // Nếu RoleId trong DB bằng 1 thì gán là "Admin", ngược lại là "Customer"
    new Claim(ClaimTypes.Role, user.RoleId == 1 ? "Admin" : "Customer")
};

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            // Tiến hành tạo phiên đăng nhập Cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            TempData["SuccessMessage"] = "Đăng nhập thành công!";

            // Điều hướng về lại trang trước đó hoặc trang chủ
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return Redirect("/");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user, string password)
        {
            var result = await _authService.RegisterAsync(user, password);
            if (!result.Success)
            {
                ViewBag.Error = result.Message;
                return View(user);
            }
            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return Redirect("/?openAuth=true");
        }

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            // Lấy kết quả xác thực từ Google
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                TempData["AuthError"] = "Đăng nhập Google thất bại!";
                return Redirect("/?openAuth=true");
            }

            // Lấy thông tin email và tên từ tài khoản Google của người dùng
            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name) ?? "Google User";

            // Kiểm tra xem email này đã có trong cơ sở dữ liệu chưa
            var user = await _authService.GetByEmailOrPhoneAsync(email);
            if (user == null)
            {
                // Kiểm tra xem bảng Roles đã có Role 'Customer' chưa, nếu có thì lấy Id, không thì mặc định là 2
                var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
                int customerRoleId = customerRole != null ? customerRole.Id : 2;

                user = new User
                {
                    Email = email,
                    FullName = name,
                    RoleId = customerRoleId, // Gán chuẩn Id của Role Customer
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // ĐỐI QUAN TRỌNG: Tạo Claims và đăng nhập bằng Cookie để hệ thống nhận diện
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.FullName ?? user.Email ?? "User"),
        new Claim(ClaimTypes.Email, user.Email ?? ""),
        new Claim(ClaimTypes.Role, user.RoleId == 1 ? "Admin" : "Customer")
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            TempData["SuccessMessage"] = "Đăng nhập Google thành công!";
            return Redirect("/");
        }

        [HttpGet]
        public IActionResult ZaloLogin()
        {
            TempData["AuthError"] = "Chức năng đăng nhập Zalo đang được bảo trì!";
            return Redirect("/?openAuth=true");
        }
    }
}