using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace TravelManager.Models;

public partial class TravelmanagerContext : DbContext
{
    public TravelmanagerContext()
    {
    }

    public TravelmanagerContext(DbContextOptions<TravelmanagerContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingPerson> BookingPersons { get; set; }

    public virtual DbSet<Credential> Credentials { get; set; }

    public virtual DbSet<Penalty> Penalties { get; set; }

    public virtual DbSet<SavedPerson> SavedPersons { get; set; }

    public virtual DbSet<Tour> Tours { get; set; }

    public virtual DbSet<TourAsset> TourAssets { get; set; }

    public virtual DbSet<TourType> TourTypes { get; set; }

    public virtual DbSet<UserDetail> UserDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PRIMARY");

            entity.ToTable("bookings");

            entity.HasIndex(e => e.CredentialId, "credential_id");

            entity.HasIndex(e => e.TourId, "tour_id");

            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.BookingDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("booking_date");
            entity.Property(e => e.Comment)
                .HasMaxLength(500)
                .HasColumnName("comment");
            entity.Property(e => e.CredentialId).HasColumnName("credential_id");
            entity.Property(e => e.NumberOfPeople)
                .HasDefaultValueSql("'1'")
                .HasColumnName("number_of_people");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasColumnType("enum('pending','confirmed','cancelled')")
                .HasColumnName("status");
            entity.Property(e => e.TotalPrice)
                .HasPrecision(10, 2)
                .HasColumnName("total_price");
            entity.Property(e => e.TourId).HasColumnName("tour_id");

            entity.HasOne(d => d.Credential).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CredentialId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("bookings_ibfk_1");

            entity.HasOne(d => d.Tour).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("bookings_ibfk_2");
        });

        modelBuilder.Entity<BookingPerson>(entity =>
        {
            entity.HasKey(e => e.PersonId).HasName("PRIMARY");

            entity.ToTable("booking_persons");

            entity.HasIndex(e => e.BookingId, "booking_id");

            entity.Property(e => e.PersonId).HasColumnName("person_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.IsChild).HasColumnName("is_child");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PassportData)
                .HasMaxLength(100)
                .HasColumnName("passport_data");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingPeople)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("booking_persons_ibfk_1");
        });

        modelBuilder.Entity<Credential>(entity =>
        {
            entity.HasKey(e => e.CredentialId).HasName("PRIMARY");

            entity.ToTable("credentials");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.Property(e => e.CredentialId).HasColumnName("credential_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Role)
                .HasColumnType("enum('client','manager','admin')")
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'active'")
                .HasColumnType("enum('active','deleted')")
                .HasColumnName("status");
        });

        modelBuilder.Entity<Penalty>(entity =>
        {
            entity.HasKey(e => e.PenaltyId).HasName("PRIMARY");

            entity.ToTable("penalties");

            entity.HasIndex(e => e.BookingId, "booking_id");

            entity.Property(e => e.PenaltyId).HasColumnName("penalty_id");
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .HasColumnName("amount");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.IsPaid)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_paid");

            entity.HasOne(d => d.Booking).WithMany(p => p.Penalties)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("penalties_ibfk_1");
        });

        modelBuilder.Entity<SavedPerson>(entity =>
        {
            entity.HasKey(e => e.PersonId).HasName("PRIMARY");

            entity.ToTable("saved_persons");

            entity.HasIndex(e => e.CredentialId, "credential_id");

            entity.Property(e => e.PersonId).HasColumnName("person_id");
            entity.Property(e => e.CredentialId).HasColumnName("credential_id");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.IsChild).HasColumnName("is_child");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PassportData)
                .HasMaxLength(100)
                .HasColumnName("passport_data");

            entity.HasOne(d => d.Credential).WithMany(p => p.SavedPeople)
                .HasForeignKey(d => d.CredentialId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("saved_persons_ibfk_1");
        });

        modelBuilder.Entity<Tour>(entity =>
        {
            entity.HasKey(e => e.TourId).HasName("PRIMARY");

            entity.ToTable("tours");

            entity.HasIndex(e => e.TypeId, "type_id");

            entity.Property(e => e.TourId).HasColumnName("tour_id");
            entity.Property(e => e.AvailableFrom).HasColumnName("available_from");
            entity.Property(e => e.AvailableTo).HasColumnName("available_to");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.DurationDays).HasColumnName("duration_days");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.TypeId).HasColumnName("type_id");

            entity.HasOne(d => d.Type).WithMany(p => p.Tours)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tours_ibfk_1");
        });

        modelBuilder.Entity<TourAsset>(entity =>
        {
            entity.HasKey(e => e.AssetId).HasName("PRIMARY");

            entity.ToTable("tour_assets");

            entity.HasIndex(e => e.TourId, "tour_id");

            entity.Property(e => e.AssetId).HasColumnName("asset_id");
            entity.Property(e => e.AssetType)
                .HasColumnType("enum('image','video')")
                .HasColumnName("asset_type");
            entity.Property(e => e.TourId).HasColumnName("tour_id");
            entity.Property(e => e.Url)
                .HasMaxLength(255)
                .HasColumnName("url");

            entity.HasOne(d => d.Tour).WithMany(p => p.TourAssets)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tour_assets_ibfk_1");
        });

        modelBuilder.Entity<TourType>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("PRIMARY");

            entity.ToTable("tour_types");

            entity.Property(e => e.TypeId).HasColumnName("type_id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<UserDetail>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.ToTable("user_details");

            entity.HasIndex(e => e.CredentialId, "credential_id").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CredentialId).HasColumnName("credential_id");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PassportData)
                .HasMaxLength(100)
                .HasColumnName("passport_data");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");

            entity.HasOne(d => d.Credential).WithOne(p => p.UserDetail)
                .HasForeignKey<UserDetail>(d => d.CredentialId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_details_ibfk_1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
