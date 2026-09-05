using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Application.Abstractions.Integrations.Asaas;

namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.Create;

internal sealed class CreateAsaasIntegrationSavedCardCommandHandler
    : BaseCommandHandler<CreateAsaasIntegrationSavedCardCommand, CreateAsaasIntegrationSavedCardResponse>
{
    private readonly IAsaasIntegrationSavedCardRepository _savedCardRepository;
    private readonly IAsaasIntegrationCustomerRepository _customerRepository;
    private readonly IAsaasIntegrationSettingRepository _settingRepository;
    private readonly IAsaasService _asaasService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAsaasIntegrationSavedCardCommandHandler(
        IAsaasIntegrationSavedCardRepository savedCardRepository,
        IAsaasIntegrationCustomerRepository customerRepository,
        IAsaasIntegrationSettingRepository settingRepository,
        IAsaasService asaasService,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _savedCardRepository = savedCardRepository;
        _customerRepository = customerRepository;
        _settingRepository = settingRepository;
        _asaasService = asaasService;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<CreateAsaasIntegrationSavedCardResponse>> Handle(
        CreateAsaasIntegrationSavedCardCommand request,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CreateAsaasIntegrationSavedCardCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                // 1. Obter as configurações de integração ativas para a empresa
                var setting = await _settingRepository.GetByBranchOrCompanyFallbackAsync(
                    request.CompanyId,
                    null,
                    cancellationToken);

                if (setting is null || !setting.IsActive || string.IsNullOrWhiteSpace(setting.ApiKeyEncrypted))
                {
                    return Result.Failure<CreateAsaasIntegrationSavedCardResponse>(
                        Error.Validation("AsaasSetting.NotFound", "Configuração de integração Asaas não configurada ou inativa para esta empresa."));
                }

                // 2. Verificar se o cliente já possui vínculo cadastrado no Asaas
                var asaasCustomer = await _customerRepository.GetByCustomerIdAndCompanyIdAsync(
                    request.CustomerId,
                    request.CompanyId,
                    cancellationToken);

                if (asaasCustomer is null)
                {
                    return Result.Failure<CreateAsaasIntegrationSavedCardResponse>(
                        Error.NotFound("AsaasCustomer.NotFound", "O cliente não possui cadastro vinculado no Asaas para esta empresa."));
                }

                // 3. Tokenizar o cartão na API do Asaas (POST /v3/creditCard/tokenizeCreditCard)
                var cardRequest = new CreditCardRequest(
                    request.HolderName,
                    request.CardNumber,
                    request.ExpiryMonth,
                    request.ExpiryYear,
                    request.Ccv);

                AsaasTokenizeCreditCardResponse tokenizedData;
                try
                {
                    tokenizedData = await _asaasService.TokenizeCreditCardAsync(
                        asaasCustomer.AsaasCustomerId,
                        cardRequest,
                        null,
                        cancellationToken);
                }
                catch (HttpRequestException ex)
                {
                    return Result.Failure<CreateAsaasIntegrationSavedCardResponse>(
                        Error.Failure("Asaas.TokenizeError", ex.Message));
                }

                // 4. Se for marcado como padrão, desmarcar cartões padrão anteriores deste cliente
                if (request.SetAsDefault)
                {
                    var existingCards = await _savedCardRepository.GetByCustomerIdAndCompanyIdForUpdateAsync(
                        request.CustomerId,
                        request.CompanyId,
                        cancellationToken);

                    foreach (var card in existingCards.Where(c => c.IsDefault))
                    {
                        card.RemoveAsDefault();
                        _savedCardRepository.Update(card);
                    }
                }

                // 5. Criar a entidade de Domínio com os dados mascarados e o token
                var savedCardResult = AsaasIntegrationSavedCard.Create(
                    request.CustomerId,
                    request.CompanyId,
                    tokenizedData.CreditCardToken,
                    tokenizedData.CreditCardBrand,
                    tokenizedData.CreditCardNumber[^4..],
                    request.HolderName,
                    request.ExpiryMonth,
                    request.ExpiryYear,
                    request.SetAsDefault);

                if (savedCardResult.IsFailure)
                    return Result.Failure<CreateAsaasIntegrationSavedCardResponse>(savedCardResult.Error);

                var savedCard = savedCardResult.Value;

                // 6. Persistência
                await _savedCardRepository.AddAsync(savedCard, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                var response = new CreateAsaasIntegrationSavedCardResponse(
                    savedCard.Id,
                    savedCard.CustomerId,
                    savedCard.CompanyId,
                    savedCard.CardBrand,
                    savedCard.Last4Digits,
                    savedCard.IsDefault);

                return Result.Success(response);
            });
    }
}