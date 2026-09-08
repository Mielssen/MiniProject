using TravelManager.Models;
using TravelManager.Repositories;
using TravelManager.Exceptions;
using TravelManager.DTOs;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;

namespace TravelManager.Services
{
    public class BookingService
    {
        private readonly BookingRepository _repo;
        private readonly TravelmanagerContext _context;
        private readonly ILogger<BookingService> _logger;
        public BookingService(BookingRepository repo, TravelmanagerContext context, ILogger<BookingService> logger)
        {
            _repo = repo;
            _context = context;
            _logger = logger;
        }

        public void Create(int credentialId, CreateBookingDto dto)
        {
            var tourExists = _repo.TourExists(dto.TourId);
            if (!tourExists)
                throw new AppException("NOT_FOUND", "Тур не знайдено");

            var tour = _repo.GetTourById(dto.TourId);

            var startDate = DateOnly.FromDateTime(dto.StartDate);
            var endDate = startDate.AddDays(tour.DurationDays);
            var availableFrom = DateOnly.FromDateTime(tour.AvailableFrom);
            var availableTo = DateOnly.FromDateTime(tour.AvailableTo);

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
            var totalPrice = (dto.NumberOfAdults * tour.Price) +
                             (dto.NumberOfChildren * tour.Price * childDiscount);

            var booking = new Booking
            {
                CredentialId = credentialId,
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
                "Нове бронювання: UserID={UserId}, TourID={TourId}, People={People}, Total={Total}",
                credentialId, dto.TourId, dto.NumberOfAdults + dto.NumberOfChildren, totalPrice);
        }

        public List<BookingDto> GetMy(int credentialId)
        {
            var bookings = _repo.GetByCredentialId(credentialId);
            return bookings.Select(b => new BookingDto
            {
                BookingId = b.BookingId,
                TourId = b.TourId,
                TourName = b.Tour.Name,
                TourPrice = b.Tour.Price,
                TourDurationDays = b.Tour.DurationDays,
                UserName = b.Credential?.UserDetail?.Name,
                UserEmail = b.Credential?.Email,
                UserPhone = b.Credential?.UserDetail?.Phone,
                UserPassportData = b.Credential?.UserDetail?.PassportData,
                UserDateOfBirth = b.Credential?.UserDetail?.DateOfBirth,
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
            }).ToList();
        }
        public void UpdateStatus(int bookingId, string status)
        {
            var booking = _repo.GetById(bookingId);

            if (booking == null)
                throw new AppException("NOT_FOUND", "Бронювання не знайдено");

            booking.Status = status;
            _repo.Update(booking);
            _logger.LogInformation(
                "Статус бронювання ID={BookingId} змінено на {Status}",
                bookingId, status);
        }
        public List<BookingDto> GetAll(string? status)
        {
            var bookings = _repo.GetAll(status);
            return bookings.Select(b => new BookingDto
            {
                BookingId = b.BookingId,
                TourId = b.TourId,
                TourName = b.Tour.Name,
                TourPrice = b.Tour.Price,
                TourDurationDays = b.Tour.DurationDays,
                UserName = b.Credential?.UserDetail?.Name,
                UserEmail = b.Credential?.Email,
                UserPhone = b.Credential?.UserDetail?.Phone,
                UserPassportData = b.Credential?.UserDetail?.PassportData,
                UserDateOfBirth = b.Credential?.UserDetail?.DateOfBirth,
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
            }).ToList();
        }
        public void Cancel(int credentialId, int bookingId)
        {
            var booking = _repo.GetById(bookingId);

            if (booking == null)
                throw new AppException("NOT_FOUND", "Бронювання не знайдено");

            if (booking.CredentialId != credentialId)
                throw new AppException("FORBIDDEN", "Це не ваше бронювання");

            if (booking.Status == "cancelled")
                throw new AppException("ALREADY_CANCELLED", "Бронювання вже скасовано");

            _context.BookingPersons.RemoveRange(
                _context.BookingPersons.Where(p => p.BookingId == bookingId)
            );
            _repo.Delete(booking);
            _logger.LogInformation(
                "Бронювання ID={BookingId} скасовано користувачем ID={UserId}",
                bookingId, credentialId);
        }

    }
}
