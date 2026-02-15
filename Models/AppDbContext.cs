using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();

        public DbSet<Comic> Comics => Set<Comic>();
        public DbSet<Chapter> Chapters => Set<Chapter>();
        public DbSet<Log> Logs => Set<Log>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // User -> Role
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comic -> User (Author)
            modelBuilder.Entity<Comic>()
                .HasOne(c => c.Author)
                .WithMany(u => u.Comics)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Chapter -> Comic
            modelBuilder.Entity<Chapter>()
                .HasOne(ch => ch.Comic)
                .WithMany(c => c.Chapters)
                .HasForeignKey(ch => ch.ComicId)
                .OnDelete(DeleteBehavior.Cascade);

            // Log -> User
            modelBuilder.Entity<Log>()
                .HasOne(l => l.User)
                .WithMany(u => u.Logs)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Table
            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Comic>().ToTable("Comic");
            modelBuilder.Entity<Chapter>().ToTable("Chapter");
            modelBuilder.Entity<Log>().ToTable("Log");

            // build role
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Moderator" },
                new Role { RoleId = 3, RoleName = "User" }
            );
        }
    }
}
