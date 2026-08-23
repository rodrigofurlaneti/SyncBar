using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Pizza.SetPizzaFlavorPrice;

internal sealed class SetPizzaFlavorPriceCommandHandler(
    IPizzaConfigurationRepository pizzaConfigurationRepository,
    IProductRepository productRepository,
    IIFoodCatalogSyncTrigger catalogSyncTrigger,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SetPizzaFlavorPriceCommand, long>(logRepository, unitOfWork)
{
    public override Task<Result<long>> Handle(SetPizzaFlavorPriceCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(SetPizzaFlavorPriceCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                var configuration = await pizzaConfigurationRepository.GetByIdForUpdateAsync(request.PizzaConfigurationId, cancellationToken);
                if (configuration is null || !configuration.IsActive)
                    return Result.Failure<long>(new Error("PizzaConfiguration.NotFound", "Pizza configuration not found."));

                var price = configuration.SetFlavorPrice(request.PizzaFlavorId, request.PizzaSizeId, request.Price);
                if (price.IsFailure)
                    return Result.Failure<long>(price.Error);

                await unitOfWork.CommitAsync(cancellationToken);

                // Diferente de AddSize/AddCrust/AddEdge: é ESTE passo que pode tornar a pizza
                // vendável pela primeira vez (1º tamanho + 1º preço de sabor) — dispara a
                // sincronização do catálogo, mesmo critério de LinkProductComplementGroupCommandHandler.
                var product = await productRepository.GetByIdAsync(configuration.ProductId, cancellationToken);
                if (product is not null)
                    catalogSyncTrigger.TriggerCompanySync(product.CompanyId);

                return Result.Success(price.Value.Id);
            });
}
