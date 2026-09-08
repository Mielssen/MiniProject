namespace TravelManager.Models
{
    public class ApiErrorResponse
    {
        public string Message { get; set; }
        public string Code { get; set; }
        public Dictionary<string, string[]> Errors { get; set; }
    }
}
