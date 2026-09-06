using SyncBar.Domain.Entities;
namespace SyncBar.Domain.Repositories
{
    public interface IKeetaIntegrationAuthorizationSessionRepository
    {
        Task<KeetaIntegrationAuthorizationSession?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationAuthorizationSession?> GetByAuthIdAsync(string authId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<KeetaIntegrationAuthorizationSession>> GetPendingSessionsAsync(CancellationToken cancellationToken = default);
        Task AddAsync(KeetaIntegrationAuthorizationSession session, CancellationToken cancellationToken = default);
        void Update(KeetaIntegrationAuthorizationSession session);
        void Delete(KeetaIntegrationAuthorizationSession session);
    }
}
