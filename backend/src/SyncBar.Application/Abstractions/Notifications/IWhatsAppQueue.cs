using SyncBar.Domain.Primitives;
namespace SyncBar.Application.Abstractions.Notifications;

public interface IWhatsAppQueue
{
    Task<Result> EnqueueAsync(string phoneNumber, string message, string fileUrl, CancellationToken cancellationToken = default);
}
