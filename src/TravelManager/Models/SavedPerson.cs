namespace TravelManager.Models;

public partial class SavedPerson
{
    public int PersonId { get; set; }

    public int? UserId { get; set; }

    public string Name { get; set; } = null!;

    public string? PassportData { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public bool IsChild { get; set; }
}