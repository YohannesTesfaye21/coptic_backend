using System.Collections.Generic;

namespace coptic_app_backend.Domain.Models
{
    public class ApiRequest
    {
        public string? HttpMethod { get; set; }
        public string? Resource { get; set; }
        public Dictionary<string, string>? PathParameters { get; set; }
        public Dictionary<string, string>? QueryStringParameters { get; set; }
        public string? Body { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
    }
}
