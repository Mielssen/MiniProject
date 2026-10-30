using System.Net;
using System.Security.Claims;
using TravelManager.DTOs;
using TravelManager.Exceptions;
using TravelManager.Repositories;

namespace TravelManager.Services
{
    public class AdminService
    {
        private readonly AdminRepository _repo;
        private readonly AuthServiceClient _authClient;
        private readonly BookingServiceClient _bookingClient;
        private readonly ILogger<AdminService> _logger;

        public AdminService(
            AdminRepository repo,
            AuthServiceClient authClient,
            BookingServiceClient bookingClient,
            ILogger<AdminService> logger)
        {
            _repo = repo;
            _authClient = authClient;
            _bookingClient = bookingClient;
            _logger = logger;
        }

        public async Task<List<ProfileDto>> GetAllUsersAsync(
            string? role,
            string? status,
            string token)
        {
            var users = await _authClient.GetAllUsersAsync(token);

            return users
                .Where(u => string.IsNullOrWhiteSpace(role) ||
                    u.Roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase)))
                .Where(u => string.IsNullOrWhiteSpace(status) ||
                    (status.Equals("active", StringComparison.OrdinalIgnoreCase)
                        ? u.IsActive
                        : status.Equals("deleted", StringComparison.OrdinalIgnoreCase)
                            && !u.IsActive))
                .Select(u => MapToDto(u))
                .ToList();
        }

        public async Task<ProfileDto> GetUserByIdAsync(int userId, string token)
        {
            var user = await _authClient.GetUserByIdAsync(userId, token);

            if (user == null)
                throw new AppException("NOT_FOUND", "Користувача не знайдено");

            return MapToDto(user);
        }

        public async Task UpdateRoleAsync(
            int adminId,
            int targetId,
            string newRole,
            string token)
        {
            if (adminId == targetId)
                throw new AppException("FORBIDDEN", "Не можна змінити власну роль");

            var user = await _authClient.GetUserByIdAsync(targetId, token);

            if (user == null)
                throw new AppException("NOT_FOUND", "Користувача не знайдено");

            await _authClient.UpdateRoleAsync(targetId, newRole, token);

            _logger.LogInformation("Адміністратор змінив роль користувача");
        }

        public async Task UpdateUserDataAsync(
            int adminId,
            int targetId,
            UpdateProfileDto dto,
            string token)
        {
            var user = await _authClient.GetUserByIdAsync(targetId, token);

            if (user == null)
                throw new AppException("NOT_FOUND", "Користувача не знайдено");

            if (!string.IsNullOrWhiteSpace(dto.PassportData) &&
                !System.Text.RegularExpressions.Regex.IsMatch(dto.PassportData, @"^\d+$"))
                throw new AppException("INVALID_PASSPORT", "Паспортні дані мають містити тільки цифри");

            if (dto.DateOfBirth.HasValue &&
                dto.DateOfBirth.Value.Date > DateTime.Today.AddYears(-18))
                throw new AppException("UNDERAGE", "Користувач має бути старше 18 років");

            await _authClient.UpdateUserDataAsync(targetId, dto, token);

            _logger.LogInformation("Адміністратор оновив дані користувача");
        }

        public async Task SetUserStatusAsync(
            int adminId,
            int targetId,
            string status,
            string token)
        {
            if (adminId == targetId)
                throw new AppException("FORBIDDEN", "Не можна деактивувати себе");

            if (status != "active" && status != "deleted")
                throw new AppException("INVALID_STATUS", "Неприпустимий статус користувача");

            var user = await _authClient.GetUserByIdAsync(targetId, token);

            if (user == null)
                throw new AppException("NOT_FOUND", "Користувача не знайдено");

            await _authClient.UpdateStatusAsync(
                targetId,
                status == "active",
                token);

            _logger.LogInformation("Адміністратор змінив статус користувача");
        }


        public async Task<StatisticsDto> GetStatisticsAsync(string token)
        {
            _logger.LogInformation("Запит статистики");

            var bookings = await _bookingClient.GetAllBookingsAsync(token);
            var users = await _authClient.GetAllUsersAsync(token);

            var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            var topTours = bookings
                .Where(b => !b.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
                .GroupBy(b => new { b.TourId, b.TourName })
                .Select(g => new TopTourDto
                {
                    TourId = g.Key.TourId,
                    Name = g.Key.TourName,
                    BookingsCount = g.Count(),
                    TotalRevenue = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(t => t.BookingsCount)
                .Take(3)
                .ToList();

            return new StatisticsDto
            {
                TotalClients = users.Count(u => u.Roles.Any(r =>
                    r.Equals("client", StringComparison.OrdinalIgnoreCase)) && u.IsActive),

                TotalManagers = users.Count(u => u.Roles.Any(r =>
                    r.Equals("manager", StringComparison.OrdinalIgnoreCase)) && u.IsActive),

                TotalUsers = users.Count(u => u.IsActive),

                TotalBookings = bookings.Count,

                PendingBookings = bookings.Count(b =>
                    b.Status.Equals("pending", StringComparison.OrdinalIgnoreCase)),

                ConfirmedBookings = bookings.Count(b =>
                    b.Status.Equals("confirmed", StringComparison.OrdinalIgnoreCase)),

                CancelledBookings = bookings.Count(b =>
                    b.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase)),

                TotalRevenue = bookings.Sum(b => b.TotalPrice),

                ConfirmedRevenue = bookings
                    .Where(b => b.Status.Equals("confirmed", StringComparison.OrdinalIgnoreCase))
                    .Sum(b => b.TotalPrice),

                TopTours = topTours,

                NewUsersThisMonth = users.Count(u => u.CreatedAt >= currentMonth),

                NewBookingsThisMonth = bookings.Count(b => b.BookingDate >= currentMonth)
            };
        }


        private static ProfileDto MapToDto(AuthUserDto user) => new()
        {
            CredentialId = user.Id,
            Email = user.Email,
            Role = user.Roles.FirstOrDefault() ?? "client",
            Status = user.IsActive ? "active" : "deleted",
            CreatedAt = user.CreatedAt,
            Name = user.DisplayName,
            Phone = user.Phone,
            PassportData = user.PassportData,
            DateOfBirth = user.DateOfBirth
        };
    }
}
