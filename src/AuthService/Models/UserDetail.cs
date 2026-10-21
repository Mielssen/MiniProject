namespace AuthService.Models;

public class UserDetail
{
    public int UserId { get; set; }

    public int CredentialId { get; set; }

    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public string? PassportData { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public Credential Credential { get; set; } = null!;
}