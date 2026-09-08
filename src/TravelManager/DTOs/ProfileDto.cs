namespace TravelManager.DTOs
{
    public class ProfileDto
    {
        public int? CredentialId { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }   
        public DateTime? CreatedAt { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string? Phone { get; set; }
        public string? PassportData { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}
