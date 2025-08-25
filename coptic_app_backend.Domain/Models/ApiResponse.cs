using System.Collections.Generic;

namespace coptic_app_backend.Domain.Models
{
    public class ApiResponse
    {
        public int StatusCode { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public string? Body { get; set; }
    }
}
