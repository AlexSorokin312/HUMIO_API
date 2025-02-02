using HUMIO_API.Model;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace YourNamespace.Data
{
    public class AppDbContext : IdentityDbContext<User> // ✅ IdentityDbContext<User> уже содержит Users
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // Таблица идентификаторов устройств
        public DbSet<DeviceIdentifier> DeviceIdentifiers { get; set; }

        // Дополнительные таблицы
        public DbSet<Trial> Trials { get; set; }
        public DbSet<Premium> Premiums { get; set; }
        public DbSet<UserStats> UserStats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Связь один-ко-многим: Пользователь - Идентификаторы устройств
            modelBuilder.Entity<DeviceIdentifier>()
                .HasOne(di => di.User)
                .WithMany(u => u.DeviceIdentifiers)
                .HasForeignKey(di => di.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Связь один-к-одному: Пользователь - Пробный период
            modelBuilder.Entity<User>()
                .HasOne(u => u.Trial)
                .WithOne(t => t.User)
                .HasForeignKey<Trial>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Связь один-к-одному: Пользователь - Премиум
            modelBuilder.Entity<User>()
                .HasOne(u => u.Premium)
                .WithOne(p => p.User)
                .HasForeignKey<Premium>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Связь один-к-одному: Пользователь - Статистика
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserStats)
                .WithOne(s => s.User)
                .HasForeignKey<UserStats>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
