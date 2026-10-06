using Microsoft.EntityFrameworkCore;
using WadnereJwellors.Domain.Entities;

namespace WadnereJwellors.DataAccess.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserRegistration> UserRegistrations { get; set; } = null!;
        public DbSet<UserOtp> UserOtps { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<GoldSilverRate> GoldSilverRates { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            });

            modelBuilder.Entity<UserRegistration>(entity =>
            {
                entity.ToTable("UserRegistration");
                entity.HasKey(e => e.RegistrationId);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EmailAddress).HasMaxLength(150);
                entity.Property(e => e.MobileNumber).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.AddressLine1).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AddressLine2).HasMaxLength(100);
                entity.Property(e => e.Village_City).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Taluka).IsRequired().HasMaxLength(100);
                entity.Property(e => e.District).IsRequired().HasMaxLength(100);
                entity.Property(e => e.State).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Pincode).IsRequired().HasMaxLength(10);
            });

            modelBuilder.Entity<UserOtp>(entity =>
            {
                entity.ToTable("UserOtps");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OtpCode).IsRequired().HasMaxLength(6);
                entity.Property(e => e.MobileNumber).IsRequired();
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired().HasMaxLength(256);
            });

            modelBuilder.Entity<GoldSilverRate>(entity =>
            {
                entity.ToTable("GoldSilverRates");
                entity.HasKey(e => e.Id);
            });
        }
    }
}
