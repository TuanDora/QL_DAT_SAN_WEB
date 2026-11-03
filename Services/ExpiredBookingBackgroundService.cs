using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MyCourtBooking.Core.Data;
using MyCourtBooking.Core.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyCourtBooking.Services
{
    public class ExpiredBookingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public ExpiredBookingBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var tenMinutesAgo = DateTime.Now.AddMinutes(-10);

                    // Tìm các đơn Chờ duyệt/Chờ thanh toán (Pending) quá 10 phút
                    var expiredBookings = await context.Bookings
                        .Where(b => b.Status == BookingStatus.Pending && b.CreatedAt <= tenMinutesAgo)
                        .ToListAsync(stoppingToken);

                    if (expiredBookings.Any())
                    {
                        foreach (var booking in expiredBookings)
                        {
                            booking.Status = BookingStatus.Expired; // Chuyển trạng thái thành Quá hạn
                        }
                        await context.SaveChangesAsync(stoppingToken);
                    }
                }

                // Cứ sau 1 phút sẽ tự động quét lại 1 lần
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}