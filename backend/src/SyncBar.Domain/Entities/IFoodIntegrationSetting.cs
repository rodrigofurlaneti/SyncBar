using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class IfoodIntegrationSetting : AggregateRoot
{
    public long CompanyId { get; private set; }
    public string? ClientId { get; private set; }
    public string? ClientSecretEncrypted { get; private set; }
    public bool Enabled { get; private set; }
    public string? IfoodCustomerId { get; private set; }
    public DateTime? LastConnectionTestAt { get; private set; }
    public bool? LastConnectionTestSucceeded { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IfoodIntegrationSetting() : base(0) { }

    private IfoodIntegrationSetting(long companyId) : base(0)
    {
        CompanyId = companyId;
        Enabled = false;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IfoodIntegrationSetting> Create(long companyId)
        => Result.Success(new IfoodIntegrationSetting(companyId));

    public Result SaveCredentials(string? clientId, string? clientSecretEncrypted, bool enabled, string? ifoodCustomerId)
    {
        ClientId = clientId;
        if (!string.IsNullOrWhiteSpace(clientSecretEncrypted))
            ClientSecretEncrypted = clientSecretEncrypted;
        Enabled = enabled;
        IfoodCustomerId = ifoodCustomerId;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    public void RegisterConnectionTest(bool succeeded)
    {
        LastConnectionTestAt = DateTime.Now;
        LastConnectionTestSucceeded = succeeded;
        UpdatedAt = DateTime.Now;
    }
}
