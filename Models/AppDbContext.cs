using Group4_ReadingComicWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace PRN222_Group4.Models
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
        }
    }
}
