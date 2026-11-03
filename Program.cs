using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using MyCourtBooking.Models.Data;
using MyCourtBooking.Models.Entities;
using MyCourtBooking.Models.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký DbContext với SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// 4. ĐĂNG KÝ CÁC SERVICE VÀO DEPENDENCY INJECTION (Sửa triệt để lỗi Unable to resolve service)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICourtService, CourtService>();
// Bổ sung các Service khác nếu dự án của bạn có (mở comment nếu sử dụng):
builder.Services.AddScoped<IBookingService, BookingService>();
// builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
}); 

// 5. Đăng ký HttpContextAccessor, Session & Controllers với Views
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddControllersWithViews();
var app = builder.Build();


// 6. Cấu hình Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Cho phép đọc file tĩnh trong wwwroot

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// 7. Định tuyến Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
// Đảm bảo dữ liệu Roles luôn tồn tại khi ứng dụng khởi chạy
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!context.Roles.Any())
    {
        context.Roles.AddRange(
            new Role { Name = "Admin", Description = "Quản trị viên hệ thống" },
            new Role { Name = "Customer", Description = "Khách hàng thông thường" }
        );
        context.SaveChanges();
    }
}
// Tự động kiểm tra và khởi tạo dữ liệu hệ thống
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // 1. Tự động tạo bảng Services và nạp 6 sản phẩm mẫu nếu chưa có trên DB hiện tại
    context.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Services')
        BEGIN
            CREATE TABLE Services (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Name NVARCHAR(250) NOT NULL,
                Category NVARCHAR(100) NULL,
                Unit NVARCHAR(50) NULL,
                Price DECIMAL(18,2) NOT NULL DEFAULT 0,
                Stock INT NOT NULL DEFAULT 0,
                IsActive BIT NOT NULL DEFAULT 1
            );

            INSERT INTO Services (Name, Category, Unit, Price, Stock, IsActive) VALUES
            (N'Nước suối Aquafina 500ml', N'Nước uống', N'Chai', 10000, 50, 1),
            (N'Nước tăng lực Redbull', N'Nước uống', N'Lon', 20000, 24, 1),
            (N'Thuê vợt Yonex', N'Dịch vụ', N'Cái', 30000, 15, 1),
            (N'Căng dây vợt cầu lông', N'Dịch vụ', N'Lần', 120000, 999, 1),
            (N'Mì cốc Hảo Hảo', N'Đồ ăn', N'Hộp', 15000, 30, 1),
            (N'Quấn cán vợt VS', N'Dịch vụ', N'Cái', 25000, 5, 1);
        END
    ");
}
app.Run();