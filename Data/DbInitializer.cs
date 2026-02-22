using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Data
{
    public static class DbInitializer
    {
        /// <summary>
        /// Seed dữ liệu mặc định cho hệ thống (Admin + Moderator).
        /// Chỉ chạy khi application start.
        /// </summary>
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            await context.Database.MigrateAsync();

            // Lấy roles
            var adminRole = await context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Admin");

            var moderatorRole = await context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Moderator");

            // =========================
            // SEED ADMIN
            // =========================
            var adminEmail = config["SeedAccounts:AdminEmail"];
            var adminPassword = config["SeedAccounts:AdminPassword"];

            if (!string.IsNullOrWhiteSpace(adminEmail) && adminRole != null)
            {
                var adminExists = await context.Users
                    .AnyAsync(u => u.Email == adminEmail);

                if (!adminExists)
                {
                    var admin = new User
                    {
                        Username = "Administrator",
                        Email = adminEmail,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                        RoleId = adminRole.RoleId,
                        Status = AccountStatus.Offline,
                        Bio = "System Administrator"
                    };

                    context.Users.Add(admin);
                    await context.SaveChangesAsync();

                    Console.WriteLine($"Created default Admin: {adminEmail}");
                }
            }

            // =========================
            // SEED MODERATOR
            // =========================
            var moderatorEmail = config["SeedAccounts:ModeratorEmail"];
            var moderatorPassword = config["SeedAccounts:ModeratorPassword"];

            if (!string.IsNullOrWhiteSpace(moderatorEmail) && moderatorRole != null)
            {
                var modExists = await context.Users
                    .AnyAsync(u => u.Email == moderatorEmail);

                if (!modExists)
                {
                    var moderator = new User
                    {
                        Username = "Moderator",
                        Email = moderatorEmail,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(moderatorPassword),
                        RoleId = moderatorRole.RoleId,
                        Status = AccountStatus.Offline,
                        Bio = "Default Moderator Account"
                    };

                    context.Users.Add(moderator);
                    await context.SaveChangesAsync();

                    Console.WriteLine($"Created default Moderator: {moderatorEmail}");
                }
            }
        }
    }
}