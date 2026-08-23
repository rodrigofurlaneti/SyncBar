using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Pizza.AddPizzaSize;

internal sealed class AddPizzaSizeCommandHandler(
    IPizzaConfigurationRepository pizzaConfigurationRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<AddPizzaSizeCommand, long>(logRepository, unitOfWork)
{
    // Campo explícito: capturar o parâmetro primário que também vai para a base dispara CS9107.
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public override Task<Result<long>> Handle(AddPizzaSizeCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(AddPizzaSizeCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                var configuration = await pizzaConfigurationRepository.GetByIdForUpdateAsync(request.PizzaConfigurationId, cancellationToken);
                if (configuration is null || !configuration.IsActive)
                    return Result.Failure<long>(new Error("PizzaConfiguration.NotFound", "Pizza configuration not found."));

                var size = configuration.AddSize(request.Name, request.Slices, request.AcceptedFractions, request.DisplayOrder);
                if (size.IsFailure)
                    return Result.Failure<long>(size.Error);

                await _unitOfWork.CommitAsync(cancellationToken);

                // Sem TriggerCompanySync aqui: um tamanho sozinho, sem nenhum PizzaFlavorPrice
                // ainda, não torna a pizza vendável — ver comentário na classe PizzaConfiguration.
                return Result.Success(size.Value.Id);
            });
}
