using MyCourtBooking.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyCourtBooking.Modules.Courts
{
    public interface ICourtService
    {
        // Các phương thức đồng bộ cũ
        List<CourtViewModel> GetAllCourts();
        CourtViewModel? GetCourtById(int id);

        // Các phương thức Bất đồng bộ (Async) làm việc với AppDbContext
        Task<(List<CourtViewModel> Courts, int TotalCount)> GetPagedCourtsAsync(
            string? keyword,
            string? district,
            int? courtTypeId,
            double? minRating,
            decimal? maxPrice,
            int page,
            int pageSize);

        Task<List<string>> GetDistrictsAsync();
        Task<List<CourtTypeViewModel>> GetCourtTypesAsync();
        Task<CourtDetailViewModel?> GetCourtDetailByIdAsync(int id);
        Task<List<CourtViewModel>> GetRelatedCourtsAsync(int currentCourtId, string district, int count);
        Task<List<SlotViewModel>> GetAvailableSlotsAsync(int courtId, DateTime date);
        Task<(bool Success, string Message)> AddReviewAsync(int userId, int courtId, double rating, string comment);
    }
}