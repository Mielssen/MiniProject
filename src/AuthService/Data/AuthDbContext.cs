using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<UserDetail> UserDetails => Set<UserDetail>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Credential>(entity =>
        {
            entity.ToTable("credentials");

            entity.HasKey(x => x.CredentialId);

            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.Property(x => x.PasswordHash)
                .IsRequired();

            entity.Property(x => x.Role)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(x => x.UserDetail)
                .WithOne(x => x.Credential)
                .HasForeignKey<UserDetail>(x => x.CredentialId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserDetail>(entity =>
        {
            entity.ToTable("user_details");

            entity.HasKey(x => x.UserId);

            entity.HasIndex(x => x.CredentialId)
                .IsUnique();

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(x => x.Phone)
                .HasMaxLength(50);

            entity.Property(x => x.PassportData)
                .HasMaxLength(255);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Token)
                .IsRequired()
                .HasMaxLength(500);

            entity.HasIndex(x => x.Token)
                .IsUnique();

            entity.HasOne(x => x.Credential)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.CredentialId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}