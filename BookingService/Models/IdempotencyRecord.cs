
namespace BookingService.Models;

public class IdempotencyRecord
{
    public string Key { get; set; } = null!;
    public string RequestHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int? BookingId { get; set; }
}
