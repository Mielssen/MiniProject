namespace AuthService.Models;

public class RefreshToken
{
    public int Id { get; set; }

    public int CredentialId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByToken { get; set; }

    public Credential Credential { get; set; } = null!;
}