using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

internal sealed class AcknowledgeIFoodOperationalAlertCommandHandler(
    IIFoodOperationalAlertStore alertStore,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<AcknowledgeIFoodOperationalAlertCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(AcknowledgeIFoodOperationalAlertCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(AcknowledgeIFoodOperationalAlertCommandHandler),
            nameof(Handle),
            null,
            (_) =>
            {
                // Idempotente de propósito: reconhecer um alerta que já sumiu (por já ter sido
                // reconhecido em outra aba, ou por ter estourado o limite de MaxPerCompany) não é
                // erro — o resultado desejado ("esse alerta não aparece mais") já está garantido.
                alertStore.Acknowledge(request.CompanyId, request.AlertId);
                return Task.FromResult(Result.Success());
            });
    }
}
