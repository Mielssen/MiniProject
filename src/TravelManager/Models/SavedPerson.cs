using System;
using System.Collections.Generic;

namespace TravelManager.Models;

public partial class SavedPerson
{
    public int PersonId { get; set; }

    public int CredentialId { get; set; }

    public string Name { get; set; } = null!;

    public string? PassportData { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public bool IsChild { get; set; }

    public virtual Credential Credential { get; set; } = null!;
}
