using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyCourtBooking.Models.Data;
using MyCourtBooking.Models.Entities;


namespace MyCourtBooking.Models.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> AuthenticateAsync(string emailOrPhone, string password)
        {
            if (string.IsNullOrWhiteSpace(emailOrPhone) || string.IsNullOrWhiteSpace(password))
                return null;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == emailOrPhone || u.PhoneNumber == emailOrPhone);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                return null;

            // Hỗ trợ khớp mật khẩu thô trong CSDL
            if (user.PasswordHash == password)
            {
                return user;
            }

            // Hỗ trợ mật khẩu đã mã hóa chuẩn
            try
            {
                var hasher = new PasswordHasher<User>();
                var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
                if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    return user;
                }
            }
            catch
            {
                // Bỏ qua lỗi định dạng hash cũ
            }

            return null;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(User user, string password)
        {
            var exists = await _context.Users.AnyAsync(u => u.Email == user.Email);
            if (exists) return (false, "Email này đã được sử dụng!");

            user.PasswordHash = HashPassword(user, password);
            user.CreatedAt = DateTime.Now;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return (true, "Đăng ký tài khoản thành công!");
        }

        public async Task<User?> GetByEmailOrPhoneAsync(string emailOrPhone)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == emailOrPhone || u.PhoneNumber == emailOrPhone);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public string HashPassword(User user, string password)
        {
            var hasher = new PasswordHasher<User>();
            return hasher.HashPassword(user, password);
        }

        public bool VerifyPassword(User user, string hashedPassword, string providedPassword)
        {
            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
            return result == PasswordVerificationResult.Success;
        }
    }
}