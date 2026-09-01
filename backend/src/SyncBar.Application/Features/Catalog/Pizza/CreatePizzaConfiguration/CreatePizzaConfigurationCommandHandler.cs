using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Pizza.CreatePizzaConfiguration;

internal sealed class CreatePizzaConfigurationCommandHandler(
    IProductRepository productRepository,
    IPizzaConfigurationRepository pizzaConfigurationRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<CreatePizzaConfigurationCommand, long>(logRepository, unitOfWork)
{
    // Campo explícito: capturar o parâmetro primário que também vai para a base dispara CS9107.
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public override Task<Result<long>> Handle(CreatePizzaConfigurationCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(CreatePizzaConfigurationCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken);
                if (product is null || !product.IsActive)
                    return Result.Failure<long>(new Error("Product.NotFound", "Product not found."));

                var existing = await pizzaConfigurationRepository.GetByProductIdAsync(request.ProductId, cancellationToken);
                if (existing is not null)
                    return Result.Success(existing.Id);

                var configuration = SyncBar.Domain.Entities.PizzaConfiguration.Create(request.ProductId);
                if (configuration.IsFailure)
                    return Result.Failure<long>(configuration.Error);

                await pizzaConfigurationRepository.AddAsync(configuration.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                // Sem TriggerCompanySync aqui: uma configuração recém-criada ainda não tem
                // tamanho/preço nenhum — não é vendável, não afeta o catálogo do Ifood ainda.
                return Result.Success(configuration.Value.Id);
            });
}
