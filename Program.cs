using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services;
using Group4_ReadingComicWeb.Hubs;
using Group4_ReadingComicWeb.Services.Contracts;
using Group4_ReadingComicWeb.Services.Implementations;
using Group4_ReadingComicWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Group4_ReadingComicWeb
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =============================
            // REGISTER SERVICES
            // =============================
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
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IHomeService, HomeService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IComicService, ComicService>();
            builder.Services.AddScoped<IPersonalComicService, PersonalComicService>();

            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IReportService, ReportService>();

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
                });

            var app = builder.Build();

            // =============================
            // SEED DATA
            // =============================
            await DbInitializer.SeedAsync(app.Services);

            // =============================
            // RESET ONLINE USERS ON START
            // =============================
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var onlineUsers = await db.Users
                    .Where(u => u.Status == AccountStatus.Online)
                    .ToListAsync();

                if (onlineUsers.Any())
                {
                    foreach (var user in onlineUsers)
                        user.Status = AccountStatus.Offline;

                    await db.SaveChangesAsync();
                }
            }

            // =============================
            // CONFIGURE PIPELINE
            // =============================
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
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapHub<UserStatusHub>("/hubs/userStatus");

            await app.RunAsync();
        }
    }
}