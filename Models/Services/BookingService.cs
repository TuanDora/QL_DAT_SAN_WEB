using Microsoft.EntityFrameworkCore;
using MyCourtBooking.Models.Data;
using MyCourtBooking.Models.Entities;
using MyCourtBooking.Models.Enums;
using MyCourtBooking.Models.DTOs;
using MyCourtBooking.Models.ViewModels;
using System.Globalization;


namespace MyCourtBooking.Models.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;
        public async Task<List<Booking>> GetUserBookingsAsync(int userId)
        {
            return await _context.Bookings
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Court)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public BookingService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. TÍNH GIÁ VÀ NHÃN KHUNG GIỜ
        // ==========================================
        public decimal GetHourlyPrice(int slotIndex)
        {
            int minutes = slotIndex * 30;

            if (minutes < 6 * 60)
                return 75000m;       // 00:00 - 06:00 (Khung đêm)

            if (minutes < 16 * 60)
                return 65000m;       // 06:00 - 16:00 (Giờ thường)

            if (minutes < 22 * 60)
                return 140000m;      // 16:00 - 22:00 (Giờ vàng cao điểm)

            return 90000m;           // 22:00 - 24:00 (Giờ tối)
        }

        public string GetPriceLabel(int slotIndex)
        {
            int minutes = slotIndex * 30;

            if (minutes < 6 * 60)
                return "Thấp điểm";

            if (minutes < 16 * 60)
                return "Giờ thường";

            if (minutes < 22 * 60)
                return "Giờ vàng";

            return "Giờ tối";
        }

        // ==========================================
        // 2. LẤY DỮ LIỆU CHỌN KHUNG GIỜ (SELECT SLOT)
        // ==========================================
        // 2. LẤY DỮ LIỆU CHỌN KHUNG GIỜ (SELECT SLOT)
        public async Task<SelectSlotViewModel?> GetSelectSlotViewModelAsync(int courtId, DateTime date)
        {
            date = date.Date;

            // Lấy thông tin sân từ CSDL (EF Core tự động nạp đầy đủ các thuộc tính của Court)
            var court = await _context.Courts
                .FirstOrDefaultAsync(c => c.Id == courtId && c.IsActive);

            if (court == null)
            {
                // Nếu không tìm thấy sân chỉ định, lấy sân hoạt động đầu tiên
                court = await _context.Courts
                    .FirstOrDefaultAsync(c => c.IsActive);
            }

            if (court == null) return null;

            // ... (Giữ nguyên các đoạn mã bên dưới của hàm)

            // Lấy danh sách các khung giờ đã được đặt trong ngày từ CSDL SQL Server (loại trừ đơn bị Hủy)
            // Lấy danh sách các khung giờ đã được đặt trong ngày từ CSDL SQL Server (loại trừ đơn bị Hủy)
            var bookedTimeStrings = await _context.BookingDetails
                .Where(bd => bd.CourtId == court.Id && bd.PlayDate.Date == date && bd.Booking.Status != BookingStatus.Cancelled)
                .Select(bd => bd.StartTime.ToString(@"hh\:mm"))
                .ToListAsync();

            var bookedSlotsSet = new HashSet<string>(bookedTimeStrings);

			var slotList = new List<MyCourtBooking.Models.ViewModels.SlotViewModel>();

			for (int index = 0; index < 48; index++)
            {
                var start = TimeSpan.FromMinutes(index * 30);
                var end = start.Add(TimeSpan.FromMinutes(30));
                string startStr = start.ToString(@"hh\:mm");

                string status = bookedSlotsSet.Contains(startStr) ? "booked" : "empty";

                // Một số slot mẫu có thể khóa bảo trì nếu muốn
                if (status == "empty" && (court.Id * 7 + index * 3) % 29 == 0)
                {
                    status = "locked";
                }

                decimal hourlyPrice = GetHourlyPrice(index);

				slotList.Add(new MyCourtBooking.Models.ViewModels.SlotViewModel
				{
                    CourtId = court.Id,
                    CourtName = court.Name,
                    SlotIndex = index,
                    StartTime = startStr,
                    EndTime = end.ToString(@"hh\:mm"),
                    Status = status,
                    PricePerHour = hourlyPrice,
                    PricePerSlot = hourlyPrice / 2m,
                    PriceLabel = GetPriceLabel(index)
                });
            }

            return new SelectSlotViewModel
            {
                CourtId = court.Id,
                CourtName = court.Name,
                CourtAddress = $"{court.Address}, {court.District}",
                District = court.District,
                BasePrice = court.BasePrice,
                SelectedDate = date,
                Slots = slotList
            };
        }

        // ==========================================
        // 3. XỬ LÝ TẠO ĐƠN ĐẶT SÂN VÀO CSDL
        // ==========================================
        public async Task<BookingResult> CreateBookingAsync(CreateBookingRequest request, int? userId)
        {
            if (request == null || request.CourtId <= 0)
            {
                return new BookingResult
                {
                    Success = false,
                    Message = "Thông tin sân đặt không hợp lệ. Vui lòng chọn lại sân!"
                };
            }

            DateTime date = request.Date.Date;

            if (date < DateTime.Today)
            {
                return BookingResult.Fail("Không thể đặt sân vào ngày đã qua.");
            }

            var courtExists = await _context.Courts.AnyAsync(c => c.Id == request.CourtId);
            if (!courtExists)
            {
                return new BookingResult
                {
                    Success = false,
                    Message = $"Sân (ID: {request.CourtId}) không tồn tại trong hệ thống!"
                };
            }

            if (request.StartTimes == null || request.StartTimes.Count == 0)
            {
                return BookingResult.Fail("Bạn chưa chọn khung giờ nào.");
            }

            // Chuyển đổi chuỗi "HH:mm" thành index 0..47
            var indexes = new List<int>();
            foreach (string time in request.StartTimes)
            {
                if (!TimeSpan.TryParseExact(time, @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan parsed))
                {
                    return BookingResult.Fail("Khung giờ không hợp lệ.");
                }

                int minutes = (int)parsed.TotalMinutes;
                if (minutes < 0 || minutes >= 1440 || minutes % 30 != 0)
                {
                    return BookingResult.Fail("Khung giờ phải theo mốc 30 phút.");
                }

                indexes.Add(minutes / 30);
            }

            indexes = indexes.Distinct().OrderBy(x => x).ToList();

            if (indexes.Count != request.StartTimes.Count)
            {
                return BookingResult.Fail("Danh sách khung giờ bị trùng lặp.");
            }

            // Kiểm tra tính liên tiếp của các slot
            for (int i = 1; i < indexes.Count; i++)
            {
                if (indexes[i] != indexes[i - 1] + 1)
                {
                    return BookingResult.Fail("Các khung giờ chọn phải liên tiếp nhau.");
                }
            }

            // Kiểm tra xung đột đặt trùng trong CSDL
            var existingBookedTimes = await _context.BookingDetails
                .Where(bd => bd.CourtId == request.CourtId && bd.PlayDate.Date == date && bd.Booking.Status != BookingStatus.Cancelled)
                .Select(bd => bd.StartTime)
                .ToListAsync();

            foreach (int index in indexes)
            {
                var slotStart = TimeSpan.FromMinutes(index * 30);
                if (existingBookedTimes.Contains(slotStart))
                {
                    return BookingResult.Fail("Khung giờ bạn chọn vừa có người khác đặt. Vui lòng chọn khung giờ khác!");
                }
            }

            // Tính tổng tiền đơn hàng
            decimal totalPrice = 0m;
            foreach (int index in indexes)
            {
                totalPrice += GetHourlyPrice(index) / 2m;
            }

            // Tạo mã đơn đặt sân duy nhất
            string bookingCode = "DS" + Guid.NewGuid().ToString("N")[..8].ToUpper();

            // 1. Tạo Entity Booking
            var booking = new Booking
            {
                BookingCode = bookingCode,
                CourtId = request.CourtId, // Gán trực tiếp Id sân đã kiểm tra
                UserId = userId,
                CustomerName = !string.IsNullOrWhiteSpace(request.CustomerName) ? request.CustomerName : "Khách đặt Online",
                CustomerPhone = request.CustomerPhone ?? "",
                BookingDate = DateTime.Now,
                TotalAmount = totalPrice,
                FinalAmount = totalPrice,
                Status = BookingStatus.Pending,
                Note = request.Note ?? "Đặt sân trực tuyến qua Website",
                CreatedAt = DateTime.Now
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // 2. Tạo các Entity BookingDetail
            foreach (int index in indexes)
            {
                var start = TimeSpan.FromMinutes(index * 30);
                var end = start.Add(TimeSpan.FromMinutes(30));

                var detail = new BookingDetail
                {
                    BookingId = booking.Id,
                    CourtId = request.CourtId,
                    PlayDate = date,
                    StartTime = start,
                    EndTime = end,
                    Price = GetHourlyPrice(index) / 2m
                };
                _context.BookingDetails.Add(detail);
            }

            // 3. Tạo Entity Payment
            var payment = new Payment
            {
                BookingId = booking.Id,
                Amount = totalPrice,
                PaymentMethod = request.PaymentMethod ?? "Tiền mặt",
                Status = BookingStatus.Completed.ToString(),
                TransactionId = "TXN_" + bookingCode,
                PaidAt = DateTime.Now
            };
            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();

            decimal durationHours = indexes.Count * 0.5m;
            string startTimeStr = TimeSpan.FromMinutes(indexes.First() * 30).ToString(@"hh\:mm");
            string endTimeStr = TimeSpan.FromMinutes((indexes.Last() + 1) * 30).ToString(@"hh\:mm");

            return new BookingResult
            {
                Success = true,
                Message = "Đặt sân thành công!",
                BookingCode = bookingCode,
                DurationHours = durationHours,
                TotalPrice = totalPrice,
                StartTime = startTimeStr,
                EndTime = endTimeStr
            };
        }
    }
}