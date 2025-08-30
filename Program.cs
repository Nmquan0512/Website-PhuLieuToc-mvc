using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Repository; // Đảm bảo namespace chứa AppDbContext và SeedData.
using PhuLieuToc.Models;

var builder = WebApplication.CreateBuilder(args);

// Thêm các service cần thiết vào container
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Cấu hình DbContext (ví dụ, dùng SQL Server hoặc thay thế theo chuỗi kết nối của bạn)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Nếu có các service khác, thêm ở đây
// ...

var app = builder.Build();

// Seed dữ liệu ban đầu nếu cần
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    SeedData.SeedingData(dbContext);
}

// Cấu hình middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Định tuyến cho Areas (phải đặt trước routing mặc định)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Định tuyến mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
