using mvcFinal2.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed Admin User
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Ensure database is created/migrated
    // context.Database.Migrate(); // Optional: depending on if you want auto-migration. usually good for dev.

    // Check if the specific admin user exists, if not create it
    if (!context.Users.Any(u => u.Email == "adminR@gmail.com"))
    {
        var adminUser = new mvcFinal2.Models.AppUser
        {
            FullName = "Admin Reyyan",
            Email = "adminR@gmail.com",
            Password = BCrypt.Net.BCrypt.HashPassword("reyyan1"),
            UserType = "Admin",
            Role = "Admin",
            University = "Yönetim",
            City = "Merkez",
            CreatedAt = DateTime.Now
        };
        context.Users.Add(adminUser);
        context.SaveChanges();
    }
}

app.Run();
