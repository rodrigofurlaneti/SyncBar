using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Security;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood;

internal sealed class TestIFoodConnectionCommandHandler : BaseCommandHandler<TestIFoodConnectionCommand, TestIFoodConnectionResponse>
{
    private const string ProtectorPurpose = "SyncBar.Integrations.IFood.ClientSecret.v1";

    private readonly IIFoodIntegrationSettingRepository _settingRepository;
    private readonly IIFoodAuthClient _authClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecretProtector _secretProtector;

    public TestIFoodConnectionCommandHandler(
        IIFoodIntegrationSettingRepository settingRepository,
        IIFoodAuthClient authClient,
        ISecretProtector secretProtector,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _settingRepository = settingRepository;
        _authClient = authClient;
        _unitOfWork = unitOfWork;
        _secretProtector = secretProtector;
    }

    public override async Task<Result<TestIFoodConnectionResponse>> Handle(
        TestIFoodConnectionCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(TestIFoodConnectionCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                var setting = await _settingRepository.GetByCompanyForUpdateAsync(request.CompanyId, cancellationToken);
                if (setting?.ClientId is null || setting.ClientSecretEncrypted is null)
                    return Result.Success(new TestIFoodConnectionResponse(
                        false, "Cadastre o Client ID e o Client Secret antes de testar a conexão."));

                string clientSecret;
                try
                {
                    clientSecret = _secretProtector.Unprotect(ProtectorPurpose, setting.ClientSecretEncrypted);
                }
                catch (Exception)
                {
                    // Chave de proteção mudou/foi perdida (ex.: reset de ambiente) — não crasha,
                    // orienta a recadastrar.
                    return Result.Success(new TestIFoodConnectionResponse(
                        false, "Não foi possível ler o segredo salvo — cadastre o Client Secret novamente."));
                }

                var auth = await _authClient.AuthenticateAsync(setting.ClientId, clientSecret, cancellationToken);

                setting.RegisterConnectionTest(auth.Success);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(new TestIFoodConnectionResponse(auth.Success, auth.ErrorMessage));
            });
    }
}
