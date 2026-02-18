using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services;
using Group4_ReadingComicWeb.Hubs;
using Group4_ReadingComicWeb.Services.Contracts;
using Group4_ReadingComicWeb.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Group4_ReadingComicWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("MyCnn")
                ));

            builder.Services.AddScoped<IEmailSender, EmailSender>();
            builder.Services.AddScoped<IModerationService, ModerationService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSignalR();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Authentication/Login";
                    options.LogoutPath = "/Authentication/Logout";
                    options.AccessDeniedPath = "/Authentication/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.AccessDeniedPath = "/Authentication/AccessDenied";
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseSession();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Authentication}/{action=Login}");

            app.MapHub<UserStatusHub>("/hubs/userStatus");

            // Reset all Online users to Offline on server start
            // (handles server restart/crash scenario)
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var onlineUsers = db.Users
                    .Where(u => u.Status == AccountStatus.Online)
                    .ToList();
                foreach (var u in onlineUsers)
                    u.Status = AccountStatus.Offline;
                if (onlineUsers.Any())
                    db.SaveChanges();
            }

            app.Run();
        }
    }
}
