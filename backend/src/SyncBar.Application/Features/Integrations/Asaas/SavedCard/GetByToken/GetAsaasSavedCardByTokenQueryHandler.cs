using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByCustomerId;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByToken
{
    internal sealed class GetAsaasSavedCardByTokenQueryHandler
        : BaseQueryHandler<GetAsaasSavedCardByTokenQuery, AsaasIntegrationSavedCardResponse>
    {
        private readonly IAsaasIntegrationSavedCardRepository _savedCardRepository;

        public GetAsaasSavedCardByTokenQueryHandler(
            IAsaasIntegrationSavedCardRepository savedCardRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _savedCardRepository = savedCardRepository;
        }

        public override async Task<Result<AsaasIntegrationSavedCardResponse>> Handle(
            GetAsaasSavedCardByTokenQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAsaasSavedCardByTokenQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var card = await _savedCardRepository.GetByTokenAsync(
                        request.CreditCardToken,
                        cancellationToken);

                    if (card is null)
                    {
                        return Result.Failure<AsaasIntegrationSavedCardResponse>(
                            Error.NotFound(
                                "AsaasSavedCard.NotFound",
                                "Cartão salvo não encontrado para o token informado."));
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
