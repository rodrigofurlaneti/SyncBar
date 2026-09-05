using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.Update
{
    internal sealed class UpdateAsaasIntegrationSavedCardCommandHandler
        : BaseCommandHandler<UpdateAsaasIntegrationSavedCardCommand>
    {
        private readonly IAsaasIntegrationSavedCardRepository _savedCardRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateAsaasIntegrationSavedCardCommandHandler(
            IAsaasIntegrationSavedCardRepository savedCardRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _savedCardRepository = savedCardRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            UpdateAsaasIntegrationSavedCardCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(UpdateAsaasIntegrationSavedCardCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var card = await _savedCardRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

                    if (card is null || card.CustomerId != request.CustomerId || card.CompanyId != request.CompanyId)
                    {
                        return Result.Failure(
                            Error.NotFound(
                                "AsaasSavedCard.NotFound",
                                $"Cartão com ID {request.Id} não foi encontrado para este cliente e empresa."));
                    }

                    // Se solicitou definir como padrão, desmarca os outros cartões do cliente nesta empresa
                    if (request.SetAsDefault == true && !card.IsDefault)
                    {
                        var existingCards = await _savedCardRepository.GetByCustomerIdAndCompanyIdForUpdateAsync(
                            request.CustomerId,
                            request.CompanyId,
                            cancellationToken);

                        foreach (var existingCard in existingCards.Where(c => c.Id != card.Id && c.IsDefault))
                        {
                            existingCard.RemoveAsDefault();
                            _savedCardRepository.Update(existingCard);
                        }

                        card.SetAsDefault();
                    }
                    else if (request.SetAsDefault == false && card.IsDefault)
                    {
                        card.RemoveAsDefault();
                    }

                    // Atualização dos dados cadastrais/validade permitidos
                    card.UpdateDetails(
                        request.HolderName ?? card.HolderName,
                        request.ExpiryMonth ?? card.ExpiryMonth,
                        request.ExpiryYear ?? card.ExpiryYear);

                    _savedCardRepository.Update(card);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
