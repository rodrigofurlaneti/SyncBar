using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class AsaasIntegrationSetting : AggregateRoot
{
    public long CompanyId { get; private set; }
    public long? BranchId { get; private set; }
    public string Environment { get; private set; } = "Sandbox";
    public string ApiKeyEncrypted { get; private set; } = string.Empty;
    public string? WebhookSecretEncrypted { get; private set; }
    public string? WalletId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private AsaasIntegrationSetting() : base(0) { }

    private AsaasIntegrationSetting(
        long companyId,
        long? branchId,
        string apiKeyEncrypted,
        string? webhookSecretEncrypted,
        string environment,
        string? walletId,
        bool isActive) : base(0)
    {
        CompanyId = companyId;
        BranchId = branchId;
        ApiKeyEncrypted = apiKeyEncrypted;
        WebhookSecretEncrypted = webhookSecretEncrypted;
        Environment = string.IsNullOrWhiteSpace(environment) ? "Sandbox" : environment;
        WalletId = walletId;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<AsaasIntegrationSetting> Create(
        long companyId,
        long? branchId,
        string apiKeyEncrypted,
        string? webhookSecretEncrypted = null,
        string environment = "Sandbox",
        string? walletId = null,
        bool isActive = true)
    {
        if (companyId <= 0)
            return Result.Failure<AsaasIntegrationSetting>(
                new Error("CompanyId.Invalid", "CompanyId inválido."));

        if (branchId.HasValue && branchId.Value <= 0)
            return Result.Failure<AsaasIntegrationSetting>(
                new Error("BranchId.Invalid", "BranchId inválido."));

        if (string.IsNullOrWhiteSpace(apiKeyEncrypted))
            return Result.Failure<AsaasIntegrationSetting>(
                new Error("ApiKey.Empty", "A chave de API é obrigatória."));

        return Result.Success(new AsaasIntegrationSetting(
            companyId,
            branchId,
            apiKeyEncrypted,
            webhookSecretEncrypted,
            environment,
            walletId,
            isActive));
    }

    public Result UpdateDetails(
        string? apiKeyEncrypted = null,
        string? webhookSecretEncrypted = null,
        string? environment = null,
        string? walletId = null,
        bool? isActive = null)
    {
        if (apiKeyEncrypted is not null)
        {
            if (string.IsNullOrWhiteSpace(apiKeyEncrypted))
                return Result.Failure(new Error("ApiKey.Empty", "A ApiKey não pode ser vazia."));

            ApiKeyEncrypted = apiKeyEncrypted;
        }

        if (webhookSecretEncrypted is not null)
            WebhookSecretEncrypted = webhookSecretEncrypted;

        if (!string.IsNullOrWhiteSpace(environment))
            Environment = environment;

        if (walletId is not null)
            WalletId = walletId;

        if (isActive.HasValue)
            IsActive = isActive.Value;

        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}