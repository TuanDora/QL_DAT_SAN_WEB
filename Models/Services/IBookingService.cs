using MyCourtBooking.Models.Entities;
using MyCourtBooking.Models.DTOs;
using MyCourtBooking.Models.ViewModels;
using System;
using System.Threading.Tasks;

namespace MyCourtBooking.Models.Services
{
    public interface IBookingService
    {
        decimal GetHourlyPrice(int slotIndex);
        string GetPriceLabel(int slotIndex);
        Task<SelectSlotViewModel?> GetSelectSlotViewModelAsync(int courtId, DateTime date);
        Task<BookingResult> CreateBookingAsync(CreateBookingRequest request, int? userId);
        Task<List<Booking>> GetUserBookingsAsync(int userId);
    }
}