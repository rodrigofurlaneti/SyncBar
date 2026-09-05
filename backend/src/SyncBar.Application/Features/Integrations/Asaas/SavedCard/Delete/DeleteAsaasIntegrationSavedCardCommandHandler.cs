using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.Delete;

internal sealed class DeleteAsaasIntegrationSavedCardCommandHandler
    : BaseCommandHandler<DeleteAsaasIntegrationSavedCardCommand>
{
    private readonly IAsaasIntegrationSavedCardRepository _savedCardRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAsaasIntegrationSavedCardCommandHandler(
        IAsaasIntegrationSavedCardRepository savedCardRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _savedCardRepository = savedCardRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(
        DeleteAsaasIntegrationSavedCardCommand request,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DeleteAsaasIntegrationSavedCardCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                // Busca o cartão garantindo isolamento por cliente e empresa
                var card = await _savedCardRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

                if (card is null || card.CustomerId != request.CustomerId || card.CompanyId != request.CompanyId)
                {
                    return Result.Failure(
                        new Error(
                            "AsaasSavedCard.NotFound",
                            $"Cartão com ID {request.Id} não foi encontrado para este cliente e empresa."));
                }

                // Deleção do registro no repositório (método Delete)
                _savedCardRepository.Delete(card);

                // Se o cartão removido era o padrão, promove o mais recente restante como padrão
                if (card.IsDefault)
                {
                    var remainingCards = await _savedCardRepository.GetByCustomerIdAndCompanyIdForUpdateAsync(
                        request.CustomerId,
                        request.CompanyId,
                        cancellationToken);

                    var nextDefault = remainingCards
                        .Where(c => c.Id != card.Id)
                        .OrderByDescending(c => c.CreatedAt)
                        .FirstOrDefault();

                    if (nextDefault is not null)
                    {
                        nextDefault.SetAsDefault();
                        _savedCardRepository.Update(nextDefault);
                    }
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success();
            });
    }
}