namespace ApiGateway.Models;

public class DashboardResponse
{
    public object? Catalog { get; set; }

    public object? Bookings { get; set; }

    public bool Partial { get; set; }

    public DashboardErrors Errors { get; set; } = new();
}

public class DashboardErrors
{
    public string? Catalog { get; set; }

    public string? Bookings { get; set; }
}