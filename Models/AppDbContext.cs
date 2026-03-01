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
        public DbSet<ComicModeration> ComicModerations { get; set; }
        public DbSet<Log> Logs => Set<Log>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ComicTag configuration
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

            // Comic default status
            modelBuilder.Entity<Comic>()
                .Property(c => c.Status)
                .HasDefaultValue("Pending");

            // User-Role relationship
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comic-Author relationship
            modelBuilder.Entity<Comic>()
                .HasOne(c => c.Author)
                .WithMany(u => u.Comics)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Chapter-Comic relationship
            modelBuilder.Entity<Chapter>()
                .HasOne(ch => ch.Comic)
                .WithMany(c => c.Chapters)
                .HasForeignKey(ch => ch.ComicId)
                .OnDelete(DeleteBehavior.Cascade);

            // ComicModeration configuration
            modelBuilder.Entity<ComicModeration>()
                .HasOne(cm => cm.Comic)
                .WithMany()
                .HasForeignKey(cm => cm.ComicId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ComicModeration>()
                .HasOne(cm => cm.Moderator)
                .WithMany()
                .HasForeignKey(cm => cm.ModeratorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ComicModeration>()
                .Property(cm => cm.ModerationStatus)
                .HasDefaultValue("Pending");

            // Log-User relationship
            modelBuilder.Entity<Log>()
                .HasOne(l => l.User)
                .WithMany(u => u.Logs)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Report entity configuration
            modelBuilder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.TargetUser)
                .WithMany()
                .HasForeignKey(r => r.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.ProcessedBy)
                .WithMany()
                .HasForeignKey(r => r.ProcessedById)
                .OnDelete(DeleteBehavior.SetNull);

            // Notification configuration
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .Property(n => n.Title)
                .HasMaxLength(255);

            // Table configurations
            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Log>().ToTable("Log");
            modelBuilder.Entity<Notification>().ToTable("Notification");

            // Seed roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Moderator" },
                new Role { RoleId = 3, RoleName = "User" }
            );
        }
    }
}