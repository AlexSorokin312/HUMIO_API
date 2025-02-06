using HUMIO_API.Requests;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HUMIO_API.DBContext
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserData> UserData { get; set; }
        public DbSet<DeviceIdentifier> DeviceIdentifiers { get; set; }
        public DbSet<UserDevice> UserDevices { get; set; }
        public DbSet<Purchase> Purchases { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User -> UserData (1 к 1)
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserData)
                .WithOne(ud => ud.User)
                .HasForeignKey<UserData>(ud => ud.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserDevice>()
                .HasKey(ud => ud.Id);

            modelBuilder.Entity<UserDevice>()
                .HasOne(ud => ud.User)
                .WithMany(u => u.UserDevices)
                .HasForeignKey(ud => ud.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserDevice>()
                .HasOne(ud => ud.DeviceIdentifier)
                .WithMany(d => d.UserDevices)
                .HasForeignKey(ud => ud.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DeviceIdentifier>()
                .HasIndex(d => d.DeviceId)
                .IsUnique();

            modelBuilder.Entity<Purchase>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
