namespace AuthService.Models;

public class Credential
{
    public int CredentialId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = "client";

    public string Status { get; set; } = "active";

    public DateTime? CreatedAt { get; set; }

    public UserDetail? UserDetail { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}