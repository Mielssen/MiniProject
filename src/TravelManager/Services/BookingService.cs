using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using TravelManager.DTOs;
using TravelManager.Exceptions;
using TravelManager.Grpc;
using TravelManager.Models;
using TravelManager.Repositories;
using Grpc.Core;

namespace TravelManager.Services
{
    public class BookingService
    {
        private readonly BookingRepository _repo;
        private readonly TravelmanagerContext _context;
        private readonly ILogger<BookingService> _logger;
        private readonly AuthServiceClient _authServiceClient;
        private readonly CatalogGrpcClient _catalogGrpcClient;
        public BookingService(BookingRepository repo, TravelmanagerContext context, ILogger<BookingService> logger, AuthServiceClient authServiceClient, CatalogGrpcClient catalogGrpcClient)
        {
            _repo = repo;
            _context = context;
            _logger = logger;
            _authServiceClient = authServiceClient;
            _catalogGrpcClient = catalogGrpcClient;
        }

        public async Task Create(int userId, string token, CreateBookingDto dto)
        {
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
            catch (global::Grpc.Core.RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.NotFound)
            {
                throw new AppException("NOT_FOUND", "Тур не знайдено");
            }

            var startDate = DateOnly.FromDateTime(dto.StartDate);
            var endDate = startDate.AddDays(tour.DurationDays);
            var availableFrom = DateOnly.FromDateTime(DateTime.Parse(tour.AvailableFrom));
            var availableTo = DateOnly.FromDateTime(DateTime.Parse(tour.AvailableTo));

            if (startDate < availableFrom)
                throw new AppException("INVALID_DATE", $"Дата початку не може бути раніше {availableFrom}");

            if (endDate > availableTo)
                throw new AppException("INVALID_DATE", $"Тур виходить за межі доступного періоду (до {availableTo})");

            if (dto.Persons.Count != dto.NumberOfAdults + dto.NumberOfChildren)
                throw new AppException("INVALID_PERSONS", "Кількість учасників не співпадає з кількістю людей");

            foreach (var person in dto.Persons)
            {
                if (string.IsNullOrWhiteSpace(person.Name))
                    throw new AppException("INVALID_PERSON", "Ім'я учасника обов'язкове");

                if (!System.Text.RegularExpressions.Regex.IsMatch(person.Name, @"^[a-zA-Zа-яА-ЯіІїЇєЄ\s]+$"))
                    throw new AppException("INVALID_PERSON", "Ім'я має містити тільки літери");

                if (string.IsNullOrWhiteSpace(person.PassportData))
                    throw new AppException("INVALID_PERSON", "Паспортні дані обов'язкові");

                if (!System.Text.RegularExpressions.Regex.IsMatch(person.PassportData, @"^\d+$"))
                    throw new AppException("INVALID_PERSON", "Паспортні дані мають містити тільки цифри");

                if (person.DateOfBirth == null)
                    throw new AppException("INVALID_PERSON", "Дата народження обов'язкова");

                if (person.DateOfBirth > DateOnly.FromDateTime(DateTime.Now))
                    throw new AppException("INVALID_PERSON", "Дата народження не може бути в майбутньому");
            }

            const decimal childDiscount = 0.5m;
            var totalPrice = (dto.NumberOfAdults * (decimal)tour.Price) +
                             (dto.NumberOfChildren * (decimal)tour.Price * childDiscount);

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

            _repo.Add(booking);

            foreach (var person in dto.Persons)
            {
                _context.BookingPersons.Add(new BookingPerson
                {
                    BookingId = booking.BookingId,
                    Name = person.Name,
                    PassportData = person.PassportData,
                    DateOfBirth = person.DateOfBirth,
                    IsChild = person.IsChild
                });
            }
            _context.SaveChanges();
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

            var bookings = _repo.GetByUserId(userId);

            var result = new List<BookingDto>();

            foreach (var b in bookings)
            {
                var tour = await _catalogGrpcClient.GetTourAsync(b.TourId);

                result.Add(new BookingDto
                {
                    BookingId = b.BookingId,
                    TourId = b.TourId,
                    TourName = tour.Name,
                    TourPrice = (decimal)tour.Price,
                    TourDurationDays = tour.DurationDays,

                    UserName = user.DisplayName,
                    UserEmail = user.Email,
                    UserPhone = user.Phone,
                    UserPassportData = user.PassportData,
                    UserDateOfBirth = user.DateOfBirth,

                    StartDate = b.StartDate,
                    BookingDate = b.BookingDate,
                    Status = b.Status,
                    NumberOfPeople = b.NumberOfPeople,
                    TotalPrice = b.TotalPrice,
                    Comment = b.Comment,

                    Persons = b.BookingPeople.Select(p => new BookingPersonDto
                    {
                        Name = p.Name,
                        PassportData = p.PassportData,
                        DateOfBirth = p.DateOfBirth,
                        IsChild = p.IsChild
                    }).ToList()
                });
            }

            return result;
        }
        public void UpdateStatus(int bookingId, string status)
        {
            var booking = _repo.GetById(bookingId);

            if (booking == null)
                throw new AppException("NOT_FOUND", "Бронювання не знайдено");

            booking.Status = status;
            _repo.Update(booking);
            _logger.LogInformation(
                "Статус бронювання змінено на {Status}", status);
        }

        public async Task<List<BookingDto>> GetAll(string? status, string token)
        {
            var bookings = _repo.GetAll(status);
            var result = new List<BookingDto>();

            foreach (var b in bookings)
            {
                var user = b.UserId.HasValue
                    ? await _authServiceClient.GetUserByIdAsync(b.UserId.Value, token)
                    : null;

                TourResponse? tour = null;

                try
                {
                    tour = await _catalogGrpcClient.GetTourAsync(b.TourId);
                }
                catch (RpcException ex)
                    when (ex.StatusCode == StatusCode.NotFound)
                {
                }

                result.Add(new BookingDto
                {
                    BookingId = b.BookingId,
                    TourId = b.TourId,
                    TourName = tour?.Name ?? "Тур недоступний",
                    TourPrice = tour is null ? 0 : (decimal)tour.Price,
                    TourDurationDays = tour?.DurationDays ?? 0,
                    UserName = user?.DisplayName,
                    UserEmail = user?.Email,
                    UserPhone = user?.Phone,
                    UserPassportData = user?.PassportData,
                    UserDateOfBirth = user?.DateOfBirth,
                    StartDate = b.StartDate,
                    BookingDate = b.BookingDate,
                    Status = b.Status,
                    NumberOfPeople = b.NumberOfPeople,
                    TotalPrice = b.TotalPrice,
                    Comment = b.Comment,
                    Persons = b.BookingPeople.Select(p => new BookingPersonDto
                    {
                        Name = p.Name,
                        PassportData = p.PassportData,
                        DateOfBirth = p.DateOfBirth,
                        IsChild = p.IsChild
                    }).ToList()
                });
            }

            return result;
        }

        public void Cancel(int userId, int bookingId)
        {
            var booking = _repo.GetById(bookingId);

            if (booking == null)
                throw new AppException("NOT_FOUND", "Бронювання не знайдено");

            if (booking.UserId != userId)
                throw new AppException("FORBIDDEN", "Це не ваше бронювання");

            if (booking.Status == "cancelled")
                throw new AppException("ALREADY_CANCELLED", "Бронювання вже скасовано");

            _context.BookingPersons.RemoveRange(
                _context.BookingPersons.Where(p => p.BookingId == bookingId)
            );

            _repo.Delete(booking);

            _logger.LogInformation("Бронювання скасовано");
        }

    }
}