using System.Text.RegularExpressions;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Exceptions;
using BookingService.Models;
using BookingService.Repositories;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using BookingService.Grpc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BookingService.Services;

public class BookingOperationsService
{
    private readonly BookingRepository _repo;
    private readonly BookingDbContext _context;
    private readonly ILogger<BookingOperationsService> _logger;
    private readonly AuthServiceClient _authServiceClient;
    private readonly CatalogGrpcClient _catalogGrpcClient;

    public BookingOperationsService(
        BookingRepository repo,
        BookingDbContext context,
        ILogger<BookingOperationsService> logger,
        AuthServiceClient authServiceClient,
        CatalogGrpcClient catalogGrpcClient)
    {
        _repo = repo;
        _context = context;
        _logger = logger;
        _authServiceClient = authServiceClient;
        _catalogGrpcClient = catalogGrpcClient;
    }


    public async Task Create(
        int userId,
        string token,
        CreateBookingDto dto,
        string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new AppException(
                "IDEMPOTENCY_KEY_REQUIRED",
                "Заголовок Idempotency-Key є обов'язковим");

        if (idempotencyKey.Length > 100)
            throw new AppException(
                "INVALID_IDEMPOTENCY_KEY",
                "Idempotency-Key не може перевищувати 100 символів");

        var requestData = JsonSerializer.Serialize(new
        {
            UserId = userId,
            dto.TourId,
            dto.StartDate,
            dto.NumberOfAdults,
            dto.NumberOfChildren,
            dto.Comment,
            dto.Persons
        });

        var requestHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(requestData)));

        var existingRecord = await _repo.GetIdempotencyRecordAsync(idempotencyKey);

        if (existingRecord != null)
        {
            if (existingRecord.RequestHash != requestHash)
                throw new AppException(
                    "IDEMPOTENCY_KEY_CONFLICT",
                    "Цей Idempotency-Key уже використано з іншими даними");

            return;
        }

        var user = await _authServiceClient.GetUserByIdAsync(userId, token);

        if (user == null)
            throw new AppException("USER_NOT_FOUND", "Користувача не знайдено");

        if (!user.IsActive)
            throw new AppException("USER_INACTIVE", "Користувач неактивний");

        TourResponse tour;

        try
        {
            tour = await _catalogGrpcClient.GetTourAsync(dto.TourId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new AppException("NOT_FOUND", "Тур не знайдено");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            throw new AppException(
                "CATALOG_TIMEOUT",
                "Каталог не відповів протягом 2 секунд. Спробуйте ще раз");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            throw new AppException(
                "CATALOG_UNAVAILABLE",
                "Сервіс каталогу зараз недоступний. Спробуйте пізніше");
        }

        var startDate = DateOnly.FromDateTime(dto.StartDate);
        var endDate = startDate.AddDays(tour.DurationDays);
        var availableFrom = DateOnly.FromDateTime(DateTime.Parse(tour.AvailableFrom));
        var availableTo = DateOnly.FromDateTime(DateTime.Parse(tour.AvailableTo));

        if (startDate < availableFrom)
            throw new AppException(
                "INVALID_DATE",
                $"Дата початку не може бути раніше {availableFrom}");

        if (endDate > availableTo)
            throw new AppException(
                "INVALID_DATE",
                $"Тур виходить за межі доступного періоду (до {availableTo})");

        if (dto.NumberOfAdults < 1 ||
            dto.NumberOfChildren < 0 ||
            dto.NumberOfAdults + dto.NumberOfChildren > 20)
            throw new AppException(
                "INVALID_PERSONS",
                "Кількість дорослих має бути від 1, а загальна кількість людей — не більше 20");

        if (dto.Persons == null ||
            dto.Persons.Count != dto.NumberOfAdults + dto.NumberOfChildren)
            throw new AppException(
                "INVALID_PERSONS",
                "Кількість учасників не співпадає з кількістю людей");

        foreach (var person in dto.Persons)
        {
            if (string.IsNullOrWhiteSpace(person.Name))
                throw new AppException("INVALID_PERSON", "Ім'я учасника обов'язкове");

            if (!Regex.IsMatch(person.Name, @"^[a-zA-Zа-яА-ЯіІїЇєЄ\s]+$"))
                throw new AppException("INVALID_PERSON", "Ім'я має містити тільки літери");

            if (string.IsNullOrWhiteSpace(person.PassportData))
                throw new AppException("INVALID_PERSON", "Паспортні дані обов'язкові");

            if (!Regex.IsMatch(person.PassportData, @"^\d+$"))
                throw new AppException("INVALID_PERSON", "Паспортні дані мають містити тільки цифри");

            if (person.DateOfBirth == null)
                throw new AppException("INVALID_PERSON", "Дата народження обов'язкова");

            if (person.DateOfBirth > DateOnly.FromDateTime(DateTime.Now))
                throw new AppException("INVALID_PERSON", "Дата народження не може бути в майбутньому");
        }

        const decimal childDiscount = 0.5m;

        var totalPrice =
            dto.NumberOfAdults * (decimal)tour.Price +
            dto.NumberOfChildren * (decimal)tour.Price * childDiscount;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var duplicate = await _repo.GetIdempotencyRecordAsync(idempotencyKey);

        if (duplicate != null)
        {
            await transaction.RollbackAsync();

            if (duplicate.RequestHash != requestHash)
                throw new AppException(
                    "IDEMPOTENCY_KEY_CONFLICT",
                    "Цей Idempotency-Key уже використано з іншими даними");

            return;
        }

        var booking = new Booking
        {
            UserId = userId,
            TourId = dto.TourId,
            StartDate = startDate,
            BookingDate = DateTime.Now,
            Status = "pending",
            NumberOfPeople = dto.NumberOfAdults + dto.NumberOfChildren,
            TotalPrice = totalPrice,
            Comment = dto.Comment
        };

        _context.Bookings.Add(booking);

        foreach (var person in dto.Persons)
        {
            booking.Persons.Add(new BookingPerson
            {
                Name = person.Name,
                PassportData = person.PassportData,
                DateOfBirth = person.DateOfBirth,
                IsChild = person.IsChild
            });
        }

        await _context.SaveChangesAsync();

        _context.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = idempotencyKey,
            RequestHash = requestHash,
            CreatedAt = DateTime.UtcNow,
            BookingId = booking.BookingId
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation(
            "Нове бронювання: TourID={TourId}, People={People}, Total={Total}",
            dto.TourId,
            dto.NumberOfAdults + dto.NumberOfChildren,
            totalPrice);
    }


    public async Task<List<BookingDto>> GetMy(int userId, string token)
    {
        var user = await _authServiceClient.GetUserByIdAsync(userId, token);

        if (user == null)
            throw new AppException("USER_NOT_FOUND", "Користувача не знайдено");

        var bookings = await _repo.GetByUserIdAsync(userId);
        var result = new List<BookingDto>();

        foreach (var booking in bookings)
        {
            TourResponse tour;

            try
            {
                tour = await _catalogGrpcClient.GetTourAsync(booking.TourId);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                throw new AppException("NOT_FOUND", "Тур не знайдено");
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
            {
                throw new AppException(
                    "CATALOG_TIMEOUT",
                    "Каталог не відповів протягом 2 секунд. Спробуйте ще раз");
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
            {
                throw new AppException(
                    "CATALOG_UNAVAILABLE",
                    "Сервіс каталогу зараз недоступний. Спробуйте пізніше");
            }

            result.Add(MapBooking(
                booking,
                tour.Name,
                (decimal)tour.Price,
                tour.DurationDays,
                user.DisplayName,
                user.Email,
                user.Phone,
                user.PassportData,
                user.DateOfBirth));
        }

        return result;
    }

    public async Task UpdateStatus(int bookingId, string status)
    {
        var booking = await _repo.GetByIdAsync(bookingId);

        if (booking == null)
            throw new AppException(
                "NOT_FOUND",
                "Бронювання не знайдено");

        booking.Status = status;
        await _repo.UpdateAsync(booking);

        _logger.LogInformation(
            "Статус бронювання ID={BookingId} змінено на {Status}",
            bookingId,
            status);
    }

    public async Task<List<BookingDto>> GetAll(string? status, string token)
    {
        var bookings = await _repo.GetAllAsync(status);
        var result = new List<BookingDto>();

        foreach (var booking in bookings)
        {
            var user = booking.UserId.HasValue
                ? await _authServiceClient.GetUserByIdAsync(booking.UserId.Value, token)
                : null;

            TourResponse? tour = null;

            try
            {
                tour = await _catalogGrpcClient.GetTourAsync(booking.TourId);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
            {
                throw new AppException(
                    "CATALOG_TIMEOUT",
                    "Каталог не відповів протягом 2 секунд. Спробуйте ще раз");
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
            {
                throw new AppException(
                    "CATALOG_UNAVAILABLE",
                    "Сервіс каталогу зараз недоступний. Спробуйте пізніше");
            }

            result.Add(MapBooking(
                booking,
                tour?.Name ?? "Тур не знайдено",
                tour == null ? 0m : (decimal)tour.Price,
                tour?.DurationDays ?? 0,
                user?.DisplayName,
                user?.Email,
                user?.Phone,
                user?.PassportData,
                user?.DateOfBirth));
        }

        return result;
    }

    public async Task Cancel(int userId, int bookingId)
    {
        var booking = await _repo.GetByIdAsync(bookingId);

        if (booking == null)
            throw new AppException(
                "NOT_FOUND",
                "Бронювання не знайдено");

        if (booking.UserId != userId)
            throw new AppException(
                "FORBIDDEN",
                "Це не ваше бронювання");

        if (booking.Status == "cancelled")
            throw new AppException(
                "ALREADY_CANCELLED",
                "Бронювання вже скасовано");

        await _repo.DeleteAsync(booking);

        _logger.LogInformation(
            "Бронювання ID={BookingId} скасовано",
            bookingId);
    }

    private static BookingDto MapBooking(
        Booking booking,
        string tourName,
        decimal tourPrice,
        int tourDurationDays,
        string? userName,
        string? userEmail,
        string? userPhone,
        string? userPassportData,
        DateTime? userDateOfBirth)
    {
        return new BookingDto
        {
            BookingId = booking.BookingId,
            TourId = booking.TourId,
            TourName = tourName,
            TourPrice = tourPrice,
            TourDurationDays = tourDurationDays,
            UserName = userName,
            UserEmail = userEmail,
            UserPhone = userPhone,
            UserPassportData = userPassportData,
            UserDateOfBirth = userDateOfBirth,
            StartDate = booking.StartDate,
            BookingDate = booking.BookingDate,
            Status = booking.Status,
            NumberOfPeople = booking.NumberOfPeople,
            TotalPrice = booking.TotalPrice,
            Comment = booking.Comment,
            Persons = booking.Persons.Select(person => new BookingPersonDto
            {
                Name = person.Name,
                PassportData = person.PassportData,
                DateOfBirth = person.DateOfBirth,
                IsChild = person.IsChild
            }).ToList()
        };
    }
}