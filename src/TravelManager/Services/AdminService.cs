using TravelManager.DTOs;
using TravelManager.Exceptions;
using TravelManager.Repositories;

namespace TravelManager.Services
{
    public class AdminService
    {
        private readonly AdminRepository _repo;
        private readonly ILogger<AdminService> _logger;

        public AdminService(AdminRepository repo, ILogger<AdminService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public List<ProfileDto> GetAllUsers(string? role, string? status)
        {
            _logger.LogInformation("Запит списку користувачів. Role={Role}, Status={Status}", role, status);

            return _repo.GetAllUsers(role, status).Select(MapToDto).ToList();
        }

        public ProfileDto GetUserById(int credentialId)
        {
            var user = _repo.GetUserById(credentialId);
            if (user == null)
            {
                _logger.LogWarning("Користувача з ID={Id} не знайдено", credentialId);
                throw new AppException("NOT_FOUND", "Користувача не знайдено");
            }
            return MapToDto(user);
        }

        public void UpdateRole(int adminId, int targetId, string newRole)
        {
            if (adminId == targetId)
                throw new AppException("FORBIDDEN", "Не можна змінити власну роль");

            var user = _repo.GetUserById(targetId);
            if (user == null)
            {
                _logger.LogWarning("Спроба змінити роль: користувача ID={Id} не знайдено", targetId);
                throw new AppException("NOT_FOUND", "Користувача не знайдено");
            }

            var oldRole = user.Role;
            user.Role = newRole;
            _repo.UpdateCredential(user);

            _logger.LogInformation(
                "Адмін ID={AdminId} змінив роль користувача ID={UserId}: {OldRole} → {NewRole}",
                adminId, targetId, oldRole, newRole);
        }

        public void UpdateUserData(int adminId, int targetId, UpdateProfileDto dto)
        {
            var user = _repo.GetUserById(targetId);
            if (user == null)
                throw new AppException("NOT_FOUND", "Користувача не знайдено");

            if (user.UserDetail == null)
                throw new AppException("NOT_FOUND", "Дані користувача не знайдено");

            if (!string.IsNullOrWhiteSpace(dto.PassportData) &&
                !System.Text.RegularExpressions.Regex.IsMatch(dto.PassportData, @"^\d+$"))
                throw new AppException("INVALID_PASSPORT", "Паспортні дані мають містити тільки цифри");

            if (dto.DateOfBirth.HasValue)
            {
                var ageLimit = DateTime.Now.AddYears(-18);
                if (dto.DateOfBirth.Value > ageLimit)
                    throw new AppException("UNDERAGE", "Користувач має бути старше 18 років");
            }

            user.UserDetail.Name = dto.Name;
            user.UserDetail.Phone = dto.Phone != null ? "+38" + dto.Phone : user.UserDetail.Phone;
            user.UserDetail.PassportData = dto.PassportData;
            if (dto.DateOfBirth.HasValue)
                user.UserDetail.DateOfBirth = dto.DateOfBirth;

            _repo.UpdateUserDetail(user.UserDetail);

            _logger.LogInformation(
                "Адмін ID={AdminId} оновив дані користувача ID={UserId}",
                adminId, targetId);
        }

        public void SetUserStatus(int adminId, int targetId, string status)
        {
            if (adminId == targetId)
                throw new AppException("FORBIDDEN", "Не можна деактивувати себе");

            var user = _repo.GetUserById(targetId);
            if (user == null)
            {
                _logger.LogWarning("Спроба змінити статус: користувача ID={Id} не знайдено", targetId);
                throw new AppException("NOT_FOUND", "Користувача не знайдено");
            }

            var oldStatus = user.Status;
            user.Status = status;
            _repo.UpdateCredential(user);

            _logger.LogInformation(
                "Адмін ID={AdminId} змінив статус користувача ID={UserId}: {OldStatus} → {NewStatus}",
                adminId, targetId, oldStatus, status);
        }

        public StatisticsDto GetStatistics()
        {
            _logger.LogInformation("Запит статистики");

            var topTours = _repo.GetTopTours(3)
                .Select(t => new TopTourDto
                {
                    TourId = t.TourId,
                    Name = t.Name,
                    BookingsCount = t.Count,
                    TotalRevenue = t.Revenue
                }).ToList();

            return new StatisticsDto
            {
                TotalClients = _repo.CountByRole("client"),
                TotalManagers = _repo.CountByRole("manager"),
                TotalUsers = _repo.CountByRole("client") + _repo.CountByRole("manager"),
                TotalBookings = _repo.CountAllBookings(),
                PendingBookings = _repo.CountBookingsByStatus("pending"),
                ConfirmedBookings = _repo.CountBookingsByStatus("confirmed"),
                CancelledBookings = _repo.CountBookingsByStatus("cancelled"),
                TotalRevenue = _repo.SumRevenue(),
                ConfirmedRevenue = _repo.SumConfirmedRevenue(),
                TopTours = topTours,
                NewUsersThisMonth = _repo.CountNewUsersThisMonth(),
                NewBookingsThisMonth = _repo.CountNewBookingsThisMonth()
            };
        }

        private static ProfileDto MapToDto(TravelManager.Models.Credential c) => new()
        {
            CredentialId = c.CredentialId,
            Email = c.Email,
            Role = c.Role,
            Status = c.Status,
            CreatedAt = c.CreatedAt,
            Name = c.UserDetail?.Name,
            Phone = c.UserDetail?.Phone,
            PassportData = c.UserDetail?.PassportData,
            DateOfBirth = c.UserDetail?.DateOfBirth
        };
    }
}