using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class UpdateRolesDto
{
    /// <summary>
    /// Список ролей, які потрібно призначити користувачу.
    /// Допустимі ролі: client, manager, admin.
    /// </summary>
    [Required]
    public List<string> Roles { get; set; } = new();
}