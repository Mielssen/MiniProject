using CatalogService.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Data;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<TourType> TourTypes => Set<TourType>();
    public DbSet<TourAsset> TourAssets => Set<TourAsset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tour>(entity =>
        {
            entity.ToTable("tours");

            entity.HasKey(e => e.TourId);

            entity.Property(e => e.TourId)
                .HasColumnName("tour_id");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(2000);

            entity.Property(e => e.Price)
                .HasColumnName("price")
                .HasPrecision(10, 2);

            entity.Property(e => e.DurationDays)
                .HasColumnName("duration_days");

            entity.Property(e => e.AvailableFrom)
                .HasColumnName("available_from");

            entity.Property(e => e.AvailableTo)
                .HasColumnName("available_to");

            entity.Property(e => e.TypeId)
                .HasColumnName("type_id");

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .ValueGeneratedOnAdd();

            entity.HasIndex(e => e.TypeId);

            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.Type)
                .WithMany(e => e.Tours)
                .HasForeignKey(e => e.TypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TourType>(entity =>
        {
            entity.ToTable("tour_types");

            entity.HasKey(e => e.TypeId);

            entity.Property(e => e.TypeId)
                .HasColumnName("type_id");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();
        });

        modelBuilder.Entity<TourAsset>(entity =>
        {
            entity.ToTable("tour_assets");

            entity.HasKey(e => e.AssetId);

            entity.Property(e => e.AssetId)
                .HasColumnName("asset_id");

            entity.Property(e => e.TourId)
                .HasColumnName("tour_id");

            entity.Property(e => e.AssetType)
                .HasColumnName("asset_type")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Url)
                .HasColumnName("url")
                .HasMaxLength(500)
                .IsRequired();

            entity.HasIndex(e => e.TourId);

            entity.HasOne(e => e.Tour)
                .WithMany(e => e.TourAssets)
                .HasForeignKey(e => e.TourId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}