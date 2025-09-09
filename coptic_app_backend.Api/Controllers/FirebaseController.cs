using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace coptic_app_backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FirebaseController : ControllerBase
    {
        private readonly ILogger<FirebaseController> _logger;
        private readonly string _firebaseBasePath;

        public FirebaseController(ILogger<FirebaseController> logger)
        {
            _logger = logger;
            _firebaseBasePath = "/app/firebase";
        }

        /// <summary>
        /// Download google-services.json for Android clients
        /// </summary>
        /// <returns>google-services.json file</returns>
        [HttpGet("google-services.json")]
        public async Task<IActionResult> GetGoogleServicesJson()
        {
            try
            {
                var filePath = Path.Combine(_firebaseBasePath, "google-services.json");
                
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("google-services.json not found at {FilePath}", filePath);
                    return NotFound("google-services.json not found");
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, "application/json", "google-services.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving google-services.json");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Download GoogleService-Info.plist for iOS clients
        /// </summary>
        /// <returns>GoogleService-Info.plist file</returns>
        [HttpGet("GoogleService-Info.plist")]
        public async Task<IActionResult> GetGoogleServiceInfoPlist()
        {
            try
            {
                var filePath = Path.Combine(_firebaseBasePath, "GoogleService-Info.plist");
                
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("GoogleService-Info.plist not found at {FilePath}", filePath);
                    return NotFound("GoogleService-Info.plist not found");
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, "application/xml", "GoogleService-Info.plist");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving GoogleService-Info.plist");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get Firebase configuration info (without sensitive data)
        /// </summary>
        /// <returns>Firebase configuration status</returns>
        [HttpGet("config")]
        public IActionResult GetFirebaseConfig()
        {
            try
            {
                var serviceAccountPath = Path.Combine(_firebaseBasePath, "service-account.json");
                var googleServicesPath = Path.Combine(_firebaseBasePath, "google-services.json");
                var googleServiceInfoPath = Path.Combine(_firebaseBasePath, "GoogleService-Info.plist");

                var config = new
                {
                    ServiceAccountAvailable = System.IO.File.Exists(serviceAccountPath),
                    GoogleServicesAvailable = System.IO.File.Exists(googleServicesPath),
                    GoogleServiceInfoAvailable = System.IO.File.Exists(googleServiceInfoPath),
                    ProjectId = Environment.GetEnvironmentVariable("FCM__ProjectId") ?? "Not configured"
                };

                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Firebase configuration");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
