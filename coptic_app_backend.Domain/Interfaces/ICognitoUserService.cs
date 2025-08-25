using System.Threading.Tasks;
using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Domain.Interfaces
{
    public interface ICognitoUserService
    {
        Task<CognitoResult> RegisterUserAsync(string email, string password, string? name, string? phoneNumber, string? deviceToken = null, string? abuneId = null);
        Task<CognitoResult> RegisterAbuneAsync(string email, string password, string? name, string? churchName, string? location = null, string? bio = null);
        Task<CognitoAuthResult> AuthenticateUserAsync(string email, string password);
        Task<CognitoResult> ConfirmUserAsync(string email, string confirmationCode);
        Task<CognitoAuthResult> RefreshTokenAsync(string refreshToken);
        Task<CognitoResult> InitiatePasswordResetAsync(string email);
        Task<CognitoResult> ResetPasswordAsync(string email, string confirmationCode, string newPassword);
                       Task<CognitoResult> LogoutUserAsync(string accessToken);
        Task<CognitoResult> ResendConfirmationCodeAsync(string email);
        Task<CognitoUserAttributes?> GetUserAttributesAsync(string email);
    }

    public class CognitoResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? UserId { get; set; }
    }

    public class CognitoAuthResult : CognitoResult
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string? IdToken { get; set; }
    }

    public class CognitoUserAttributes
    {
        public string? Name { get; set; }
        public string? Gender { get; set; }
        public string? Email { get; set; }
    }
}
