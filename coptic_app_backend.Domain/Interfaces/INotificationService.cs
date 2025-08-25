using System.Threading.Tasks;

namespace coptic_app_backend.Domain.Interfaces
{
    public interface INotificationService
    {
        Task<bool> SendNotificationAsync(string userId, string title, string body);
        Task<bool> SendNotificationToAllAsync(string title, string body);
    }
}
