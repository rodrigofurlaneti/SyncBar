using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByCustomerId;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByIdForUpdate
{
    internal sealed class GetAsaasSavedCardByIdForUpdateQueryHandler
        : BaseQueryHandler<GetAsaasSavedCardByIdForUpdateQuery, AsaasIntegrationSavedCardResponse>
    {
        private readonly IAsaasIntegrationSavedCardRepository _savedCardRepository;

        public GetAsaasSavedCardByIdForUpdateQueryHandler(
            IAsaasIntegrationSavedCardRepository savedCardRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _savedCardRepository = savedCardRepository;
        }

        public override async Task<Result<AsaasIntegrationSavedCardResponse>> Handle(
            GetAsaasSavedCardByIdForUpdateQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAsaasSavedCardByIdForUpdateQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var card = await _savedCardRepository.GetByIdForUpdateAsync(
                        request.Id,
                        cancellationToken);

                    if (card is null)
                    {
                        return Result.Failure<AsaasIntegrationSavedCardResponse>(
                            Error.NotFound(
                                "AsaasSavedCard.NotFound",
                                $"Cartão salvo com ID {request.Id} não foi encontrado para atualização."));
                    }

                    var response = new AsaasIntegrationSavedCardResponse(
                        card.Id,
                        card.CustomerId,
                        card.CompanyId,
                        card.CardBrand,
                        card.Last4Digits,
                        card.HolderName,
                        card.ExpiryMonth,
                        card.ExpiryYear,
                        card.IsDefault,
                        card.CreatedAt,
                        card.IsActive);

                    return Result.Success(response);
                });
        }
    }
}
