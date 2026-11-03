using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using MyCourtBooking.Core.Data;
using MyCourtBooking.Modules.Bookings;
using MyCourtBooking.Modules.Courts;
using MyCourtBooking.Modules.Shared;
using MyCourtBooking.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký DbContext với SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=localhost;Database=MyCourtBookingDb;Trusted_Connection=True;TrustServerCertificate=True;"
    ));

// 2. Cấu hình Custom View Location cho Kiến trúc Module
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new ModuleViewLocationExpander());
});

// 3. Cấu hình Authentication (Cookie & Google)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = "915168180496-ctg7hq4bcv0inkjm0tbukm572imhr1ov.apps.googleusercontent.com";
    options.ClientSecret = "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";
});

// 4. Đăng ký các Service vào Dependency Injection
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICourtService, CourtService>();
builder.Services.AddScoped<IBookingService, BookingService>();

// 5. Đăng ký HttpContextAccessor, Session & Controllers với Views
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

builder.Services.AddHostedService<MyCourtBooking.Services.ExpiredBookingBackgroundService>();

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

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// 7. Định tuyến Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();