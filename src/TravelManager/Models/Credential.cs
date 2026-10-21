namespace TravelManager.Models;

public partial class Credential
{
    public int CredentialId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual UserDetail? UserDetail { get; set; }
}