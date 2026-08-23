using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Pizza.AddPizzaCrust;

internal sealed class AddPizzaCrustCommandHandler(
    IPizzaConfigurationRepository pizzaConfigurationRepository,
    IProductRepository productRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<AddPizzaCrustCommand, long>(logRepository, unitOfWork)
{
    // Campo explícito: capturar o parâmetro primário que também vai para a base dispara CS9107.
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public override Task<Result<long>> Handle(AddPizzaCrustCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(AddPizzaCrustCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                var configuration = await pizzaConfigurationRepository.GetByIdForUpdateAsync(request.PizzaConfigurationId, cancellationToken);
                if (configuration is null || !configuration.IsActive)
                    return Result.Failure<long>(new Error("PizzaConfiguration.NotFound", "Pizza configuration not found."));

                var product = await productRepository.GetByIdAsync(configuration.ProductId, cancellationToken);
                if (product is null)
                    return Result.Failure<long>(new Error("PizzaConfiguration.NotFound", "Pizza configuration not found."));

                var crust = configuration.AddCrust(request.Name, request.ExtraPrice, request.DisplayOrder);
                if (crust.IsFailure)
                    return Result.Failure<long>(crust.Error);

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success(crust.Value.Id);
            });
}
