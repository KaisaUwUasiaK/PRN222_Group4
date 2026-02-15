using Group4_ReadingComicWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Comic> Comics { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ComicTag> ComicTags { get; set; }

        public DbSet<Log> Logs => Set<Log>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ComicTag>()
                .HasKey(ct => new { ct.ComicId, ct.TagId });

            modelBuilder.Entity<ComicTag>()
                .HasOne(ct => ct.Comic)
                .WithMany(c => c.ComicTags)
                .HasForeignKey(ct => ct.ComicId);

            modelBuilder.Entity<ComicTag>()
                .HasOne(ct => ct.Tag)
                .WithMany(t => t.ComicTags)
                .HasForeignKey(ct => ct.TagId);

            modelBuilder.Entity<Comic>()
                .Property(c => c.Status)
                .HasDefaultValue("Pending");

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comic>()
                .HasOne(c => c.Author)
                .WithMany(u => u.Comics)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Chapter>()
                .HasOne(ch => ch.Comic)
                .WithMany(c => c.Chapters)
                .HasForeignKey(ch => ch.ComicId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Log>()
                .HasOne(l => l.User)
                .WithMany(u => u.Logs)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Log>().ToTable("Log");

            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Moderator" },
                new Role { RoleId = 3, RoleName = "User" }
            );
        }
    }
}
