using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByCustomerId;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetById
{
    internal sealed class GetAsaasSavedCardByIdQueryHandler
        : BaseQueryHandler<GetAsaasSavedCardByIdQuery, AsaasIntegrationSavedCardResponse>
    {
        private readonly IAsaasIntegrationSavedCardRepository _savedCardRepository;

        public GetAsaasSavedCardByIdQueryHandler(
            IAsaasIntegrationSavedCardRepository savedCardRepository,
            ILogTrackerRepository logRepository)
            : base(logRepository)
        {
            _savedCardRepository = savedCardRepository;
        }

        public override async Task<Result<AsaasIntegrationSavedCardResponse>> Handle(
            GetAsaasSavedCardByIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAsaasSavedCardByIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var card = await _savedCardRepository.GetByIdAsync(
                        request.Id,
                        cancellationToken);

                    if (card is null)
                    {
                        return Result.Failure<AsaasIntegrationSavedCardResponse>(
                            Error.NotFound(
                                "AsaasSavedCard.NotFound",
                                $"Cartão salvo com ID {request.Id} não foi encontrado."));
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
