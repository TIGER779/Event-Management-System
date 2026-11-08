using AreebTechnologyTask.Models;
using Microsoft.EntityFrameworkCore;

namespace AreebTechnologyTask.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        // The connection string and provider are configured in Program.cs (AddDbContext),
        // from the "ConnectionStrings:Default" setting.
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table names
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Event>().ToTable("Events");
            modelBuilder.Entity<Booking>().ToTable("Bookings");

            // User table config
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Name).HasMaxLength(100).IsRequired();
                entity.Property(u => u.Email).HasColumnType("nvarchar").HasMaxLength(100).IsRequired();
                entity.Property(u => u.HashedPassword).HasMaxLength(100).IsRequired();
                entity.Property(u => u.Role).HasColumnType("nvarchar").HasMaxLength(10).IsRequired();
                entity.Property(u => u.Address).HasMaxLength(100).IsRequired();
                entity.Property(u => u.Phone).HasMaxLength(20).IsRequired();
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            });

            // Event table config
            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasColumnType("nvarchar(max)").IsRequired();
                entity.Property(e => e.Category).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Venue).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ImageUrl).HasMaxLength(255).IsRequired();
            });

            // Booking table config
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasOne(b => b.User)
                      .WithMany(u => u.Bookings)
                      .HasForeignKey(b => b.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(b => b.Event)
                      .WithMany(e => e.Bookings)
                      .HasForeignKey(b => b.EventId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
