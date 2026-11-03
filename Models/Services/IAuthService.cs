using MyCourtBooking.Models.Entities;
using System.Threading.Tasks;

namespace MyCourtBooking.Models.Services
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string emailOrPhone, string password);
        Task<(bool Success, string Message)> RegisterAsync(User user, string password);
        Task<User?> GetByEmailOrPhoneAsync(string emailOrPhone);
        Task<User?> GetByIdAsync(int id);
        string HashPassword(User user, string password);
        bool VerifyPassword(User user, string hashedPassword, string providedPassword);
    }
}