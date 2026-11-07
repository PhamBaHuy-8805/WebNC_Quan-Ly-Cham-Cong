
using Microsoft.EntityFrameworkCore;
using BTL_WebNC_Quản_lý_chấm_công.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using BTL_WebNC_Quản_lý_chấm_công.Models;
using BTL_WebNC_Quản_lý_chấm_công.Services;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// 1. MVC
builder.Services.AddControllersWithViews();

// 2. COOKIE AUTHENTICATION 
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

// 3. SERVICES 
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddMemoryCache();

builder.Services.AddScoped<
    BTL_WebNC_Quản_lý_chấm_công.Services.PayrollService>();

builder.Services.AddScoped<
    BTL_WebNC_Quản_lý_chấm_công.Services.ReportService>();

// 4. DATABASE CHẤM CÔNG 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);
// IDENTITY CORE 
builder.Services
    .AddIdentityCore<Account>()
    .AddUserStore<ExistingAccountUserStore>();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = ".BTLWebNC.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
// 5. BUILD APPLICATION
var app = builder.Build();

// 6. SEED DATA HIỆN TẠI
if (args.Contains("--seed"))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Dữ liệu demo chỉ được seed trong môi trường Development."
        );
    }

    await using var scope = app.Services.CreateAsyncScope();

    var context = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await SeedData.InitializeAsync(context);

    app.Logger.LogInformation(
        "Seed dữ liệu mẫu hoàn tất. Chạy lại không thêm trùng các bản ghi đã có."
    );

    return;
}

// 7. HTTP REQUEST PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
// Xác thực và phân quyền 
app.UseAuthentication();
app.UseAuthorization();

// MVC Routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
