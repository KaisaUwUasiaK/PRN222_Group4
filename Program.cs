using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services;
using Group4_ReadingComicWeb.Hubs;
using Group4_ReadingComicWeb.Services.Contracts;
using Group4_ReadingComicWeb.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using BCrypt.Net;

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
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
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
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapHub<UserStatusHub>("/hubs/userStatus");

            // Reset all Online users to Offline on server start
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

            // ✅ SEED DATA CHO ADMIN VÀ MODERATOR
            using (var scope = app.Services.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;
                var context = serviceProvider.GetRequiredService<AppDbContext>();
                var config = serviceProvider.GetRequiredService<IConfiguration>();

                // Lấy roles
                var adminRole = context.Roles.FirstOrDefault(r => r.RoleName == "Admin");
                var moderatorRole = context.Roles.FirstOrDefault(r => r.RoleName == "Moderator");

                // ============================================
                // SEED ADMIN ACCOUNT
                // ============================================
                var adminEmail = config["SeedAccounts:AdminEmail"];
                var adminPassword = config["SeedAccounts:AdminPassword"];

                if (!string.IsNullOrEmpty(adminEmail) && adminRole != null)
                {
                    var adminExists = context.Users.Any(u => u.Email == adminEmail);
                    if (!adminExists)
                    {
                        var admin = new User
                        {
                            Username = "Administrator",
                            Email = adminEmail,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                            RoleId = adminRole.RoleId,
                            Status = AccountStatus.Offline,
                            Bio = "System Administrator",
                            AvatarUrl = null
                        };

                        context.Users.Add(admin);
                        context.SaveChanges();
                        Console.WriteLine($"✅ Created default Admin: {adminEmail}");
                    }
                    else
                    {
                        Console.WriteLine($"ℹ️  Admin already exists: {adminEmail}");
                    }
                }

                // ============================================
                // SEED MODERATOR ACCOUNT
                // ============================================
                var moderatorEmail = config["SeedAccounts:ModeratorEmail"];
                var moderatorPassword = config["SeedAccounts:ModeratorPassword"];

                if (!string.IsNullOrEmpty(moderatorEmail) && moderatorRole != null)
                {
                    var moderatorExists = context.Users.Any(u => u.Email == moderatorEmail);
                    if (!moderatorExists)
                    {
                        var moderator = new User
                        {
                            Username = "Moderator",
                            Email = moderatorEmail,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(moderatorPassword),
                            RoleId = moderatorRole.RoleId,
                            Status = AccountStatus.Offline,
                            Bio = "Default Moderator Account",
                            AvatarUrl = null
                        };

                        context.Users.Add(moderator);
                        context.SaveChanges();
                        Console.WriteLine($"✅ Created default Moderator: {moderatorEmail}");
                    }
                    else
                    {
                        Console.WriteLine($"ℹ️  Moderator already exists: {moderatorEmail}");
                    }
                }
            }

            app.Run();
        }
    }
}
