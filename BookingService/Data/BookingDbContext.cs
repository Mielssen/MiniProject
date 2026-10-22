using BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<BookingPerson> BookingPersons => Set<BookingPerson>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("bookings");

            entity.HasKey(b => b.BookingId);

            entity.Property(b => b.BookingId).HasColumnName("booking_id");
            entity.Property(b => b.UserId).HasColumnName("user_id");
            entity.Property(b => b.TourId).HasColumnName("tour_id");
            entity.Property(b => b.StartDate).HasColumnName("start_date");
            entity.Property(b => b.BookingDate).HasColumnName("booking_date");
            entity.Property(b => b.Status).HasColumnName("status").HasMaxLength(30);
            entity.Property(b => b.NumberOfPeople).HasColumnName("number_of_people");
            entity.Property(b => b.TotalPrice).HasColumnName("total_price").HasPrecision(10, 2);
            entity.Property(b => b.Comment).HasColumnName("comment").HasMaxLength(500);

            entity.HasMany(b => b.Persons)
                .WithOne(p => p.Booking)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BookingPerson>(entity =>
        {
            entity.ToTable("booking_persons");

            entity.HasKey(p => p.PersonId);

            entity.Property(p => p.PersonId).HasColumnName("person_id");
            entity.Property(p => p.BookingId).HasColumnName("booking_id");
            entity.Property(p => p.Name).HasColumnName("name").HasMaxLength(200);
            entity.Property(p => p.PassportData).HasColumnName("passport_data").HasMaxLength(50);
            entity.Property(p => p.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(p => p.IsChild).HasColumnName("is_child");
        });


        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable("idempotency_records");

            entity.HasKey(r => r.Key);

            entity.Property(r => r.Key)
                .HasColumnName("key")
                .HasMaxLength(100);

            entity.Property(r => r.RequestHash)
                .HasColumnName("request_hash")
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(r => r.CreatedAt)
                .HasColumnName("created_at");

            entity.Property(r => r.BookingId)
                .HasColumnName("booking_id");
        });

    }
}