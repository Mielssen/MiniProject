namespace AuthService.DTOs;

public class ProfileDto
{
    /// <summary>
    /// Унікальний ідентифікатор користувача.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Email користувача.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Відображуване ім'я користувача.
    /// </summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Номер телефону користувача.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Паспортні дані користувача.
    /// </summary>
    public string? PassportData { get; set; }

    /// <summary>
    /// Дата народження користувача.
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Дата створення облікового запису.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Ознака активності облікового запису.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Список ролей користувача.
    /// </summary>
    public List<string> Roles { get; set; } = new();
}