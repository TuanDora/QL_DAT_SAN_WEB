using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using MyCourtBooking.Core.Data;
using MyCourtBooking.Core.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyCourtBooking.Models.Services;

namespace MyCourtBooking.Modules.Auth
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context; // Đảm bảo đã khai báo biến này

        // Constructor phải gán giá trị cho cả _authService và _context
        public AuthController(IAuthService authService, ApplicationDbContext context)
        {
            _authService = authService;
            _context = context; // <--- Dòng này rất quan trọng để _context không bị null
        }

        // ViewModel nhận dữ liệu trực tiếp từ form chuẩn
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _authService.GetByEmailOrPhoneAsync(email);
            if (user == null)
            {
                ViewBag.Error = "Không tìm thấy tài khoản với email này!";
                return View();
            }
            ViewBag.Success = "Yêu cầu khôi phục mật khẩu đã được gửi đến email của bạn!";
            return View();
        }

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