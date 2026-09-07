using SyncBar.Domain.Primitives;

namespace SyncBar.Application.Abstractions.Notifications
{
    public interface IWhatsAppService
    {
        Task<Result> SendImageAsync(string phoneNumber, string message, string fileUrl, CancellationToken cancellationToken = default);
    }
}
