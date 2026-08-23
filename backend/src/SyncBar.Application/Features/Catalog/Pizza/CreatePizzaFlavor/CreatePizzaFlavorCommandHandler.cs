using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Pizza.CreatePizzaFlavor;

internal sealed class CreatePizzaFlavorCommandHandler(
    IPizzaFlavorRepository pizzaFlavorRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<CreatePizzaFlavorCommand, long>(logRepository, unitOfWork)
{
    // Campo explícito: capturar o parâmetro primário que também vai para a base dispara CS9107.
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public override Task<Result<long>> Handle(CreatePizzaFlavorCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(CreatePizzaFlavorCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                var flavor = PizzaFlavor.Create(request.CompanyId, request.Name, request.Description);
                if (flavor.IsFailure)
                    return Result.Failure<long>(flavor.Error);

                await pizzaFlavorRepository.AddAsync(flavor.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                // Sem TriggerCompanySync aqui: um sabor sozinho (sem preço em nenhuma
                // PizzaConfiguration) não afeta o catálogo do iFood ainda — mesmo critério de
                // CreateComplementItemCommandHandler.
                return Result.Success(flavor.Value.Id);
            });
}
