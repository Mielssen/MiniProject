using BookingService.Data;
using BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Repositories;

public class BookingRepository
{
    private readonly BookingDbContext _context;

    public BookingRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(int id)
    {
        return await _context.Bookings
            .Include(b => b.Persons)
            .FirstOrDefaultAsync(b => b.BookingId == id);
    }

    public async Task<List<Booking>> GetByUserIdAsync(int userId)
    {
        return await _context.Bookings
            .Include(b => b.Persons)
            .Where(b => b.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetAllAsync(string? status)
    {
        var query = _context.Bookings
            .Include(b => b.Persons)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(b => b.Status == status);

        return await query.ToListAsync();
    }

    public async Task AddAsync(Booking booking)
    {
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Booking booking)
    {
        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Booking booking)
    {
        _context.Bookings.Remove(booking);
        await _context.SaveChangesAsync();
    }

    public async Task<IdempotencyRecord?> GetIdempotencyRecordAsync(string key)
    {
        return await _context.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.Key == key);
    }

    public async Task AddIdempotencyRecordAsync(IdempotencyRecord record)
    {
        _context.IdempotencyRecords.Add(record);
        await _context.SaveChangesAsync();
    }
}